using Microsoft.EntityFrameworkCore;
using RadarService.DbContextClass;
using TrackLibrary;

namespace RadarService.Services
{
    public class TrackManagerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _expireTime = TimeSpan.FromSeconds(30);
        private readonly Random _random = new Random();
        private readonly KafkaProducerService _kafkaProducer;

        public TrackManagerService(IServiceScopeFactory scopeFactory, KafkaProducerService kafkaProducer)
        {
            _scopeFactory = scopeFactory;
            _kafkaProducer = kafkaProducer;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var currentTrackCount = await context.Tracks.CountAsync();
                    Console.WriteLine($"Mevcut track sayısı: {currentTrackCount}");

                    if (currentTrackCount < 25)
                    {
                        var newTrack = new Track
                        {
                            Latitude = _random.NextDouble() * (46 - 27) + 27,
                            Longitude = _random.NextDouble() * (50 - 20) + 20,
                            Altitude = _random.NextDouble() * 10000,
                            Angle = _random.NextDouble() * 360,
                            Speed = _random.NextDouble() * 100,
                            Environment = GenerateEnvironmentType(),
                            LastUpdate = DateTime.UtcNow
                            // IFF bilgileri null olarak kalacak - IFF servisi tarafından doldurulacak
                        };

                        Console.WriteLine($"Yeni track oluşturuluyor: Lat={newTrack.Latitude:F2}, Lon={newTrack.Longitude:F2}");

                        context.Tracks.Add(newTrack);
                        await context.SaveChangesAsync(); // Önce kaydet ki ID oluşsun

                        Console.WriteLine($"Track MongoDB'ye kaydedildi, ID: {newTrack.Id}");
                        Console.WriteLine($"Created: {newTrack.Id} numaralı track oluşturuldu!");

                        try
                        {
                            
                            await _kafkaProducer.NotifyTrackAsync(newTrack.Id.ToString(), "new_track"); // new_track topic
                            Console.WriteLine($"Track 'created' notification gönderildi: ID {newTrack.Id}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Kafka create notification hatası: {ex.Message}");
                        }
                    }

                    // 2. Var olan izleri güncelle

                    var tracks = await context.Tracks.ToListAsync(stoppingToken);

                    var randomTrack = _random.Next(1,currentTrackCount+1);

                    var shuffledTrack = context.Tracks.OrderBy(x => _random.Next()).Take(randomTrack);

                    foreach (var track in shuffledTrack)
                    {

                        track.Latitude += (_random.NextDouble() - 0.5) * track.Speed * 0.01;
                        track.Longitude += (_random.NextDouble() - 0.5) * track.Speed * 0.01;
                        track.LastUpdate = DateTime.Now;
                    }

                    // Önce değişiklikleri kaydet
                    await context.SaveChangesAsync(stoppingToken);

                    // Track Updated Notifications gönder
                    foreach (var track in shuffledTrack)
                    {
                        try
                        {
                            await _kafkaProducer.NotifyTrackAsync(track.Id.ToString(), "update_track"); // track_updated topic
                            Console.WriteLine($" Track 'updated' notification gönderildi: ID {track.Id}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Track update notification hatası: {ex.Message}");
                        }
                    }
                    // 3. Eski izleri sil

                    var expireTime = DateTime.UtcNow - _expireTime;
                    var expiredTracks = tracks.Where(t => t.LastUpdate < expireTime).ToList();

                    if (expiredTracks.Any())
                    {
                        context.Tracks.RemoveRange(expiredTracks);
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                await Task.Delay(300);
            }
        }

        private string GenerateEnvironmentType()
        {
            var environments = new[] { "air", "ground", "sea surface", "sea bottom", "space" };
            return environments[_random.Next(environments.Length)];
        }
    }
}


