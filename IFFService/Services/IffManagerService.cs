using IFFService.DbContextClass;
using TrackLibrary;
using Microsoft.EntityFrameworkCore;
using Confluent.Kafka;
using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Bson;

namespace IFFService.Services
{
    public class IffManagerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly KafkaProducerService _kafkaProducer;
        private readonly ILogger<IffManagerService> _logger;
        private IConsumer<Ignore, string>? _consumer;

        public IffManagerService(IServiceScopeFactory scopeFactory, KafkaProducerService kafkaProducer, ILogger<IffManagerService> logger)
        {
            _scopeFactory = scopeFactory;
            _kafkaProducer = kafkaProducer;
            _logger = logger;
        }

        private void InitializeConsumer()
        {
            if (_consumer == null)
            {
                var consumerConfig = new ConsumerConfig
                {
                    BootstrapServers = "localhost:9092",
                    GroupId = "iff-service",
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    SessionTimeoutMs = 30000,
                    EnableAutoCommit = false
                };
                _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
                _consumer.Subscribe(new[] { "new_track" });
                _logger.LogInformation("IFF Service Kafka Consumer yapılandırıldı - new_track topic dinleniyor");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("IFF Manager Service başlatıldı, RadarService'den 'new_track' topic'ini dinliyor...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    InitializeConsumer();
                    
                    var result = _consumer.Consume(TimeSpan.FromMilliseconds(1000));
                    
                    if (result != null)
                    {
                        _logger.LogInformation($"Kafka'dan yeni track mesajı alındı: {result.Message.Value}, Topic: {result.Topic}");

                        // Mesaj direkt TrackId string'i
                        string trackId = result.Message.Value;

                        if (!string.IsNullOrEmpty(trackId) && result.Topic == "new_track")
                        {
                            _logger.LogInformation($"Yeni track event işleniyor: ID {trackId}");

                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                                // Track'i MongoDB'den çek - EntityFramework FindAsync kullan
                                if (ObjectId.TryParse(trackId, out ObjectId objectId))
                                {
                                    var track = await context.Tracks.FindAsync(new object[] { objectId }, stoppingToken);

                                    if (track != null)
                                    {
                                        // IFF analizi yap
                                        var enrichedTrack = await AnalyzeAsync(track, context);
                                        
                                        // WebSocketService'e IFF enriched track'i gönder
                                        await _kafkaProducer.NotifyTrackAsync(enrichedTrack.Id.ToString(), "update_track");
                                        
                                        _logger.LogInformation($"IFF Analysis tamamlandı ve update_track topic'ine gönderildi: {enrichedTrack.IffType} - {enrichedTrack.Country} - {enrichedTrack.Callsign}");
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"Track MongoDB'de bulunamadı: ID {trackId}");
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning($"Geçersiz ObjectId formatı: {trackId}");
                                }
                            }

                            _consumer.Commit(result);
                        }
                        else
                        {
                            _logger.LogWarning($"TrackId null veya geçersiz topic: Topic={result.Topic}, TrackId={trackId}");
                        }
                    }
                    
                    await Task.Delay(100, stoppingToken);
                }
                catch (ConsumeException ex) when (ex.Message.Contains("Unknown topic") || ex.Message.Contains("not available"))
                {
                    _logger.LogError($"Topic hatası: new_track topic'i bulunamadı. 5 saniye bekleniyor... Hata: {ex.Message}");
                    
                    // Consumer'ı sıfırla
                    _consumer?.Close();
                    _consumer?.Dispose();
                    _consumer = null;
                    
                    await Task.Delay(5000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Event processing hatası: {ex.Message}");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private async Task<Track> AnalyzeAsync(Track incomingTrack, ApplicationDbContext dbContext)
        {
            // EntityFramework FindAsync kullan
            var existingTrack = await dbContext.Tracks.FindAsync(incomingTrack.Id);

            if (existingTrack == null)
            {
                _logger.LogWarning($"Track ID {incomingTrack.Id} bulunamadı veritabanında!");
                return incomingTrack;
            }

            // IFF analizi - Latitude'a göre IFF bilgilerini belirle
            if (existingTrack.Latitude > 40)
            {
                existingTrack.IffType = "Ally";
                existingTrack.Country = "Turkiye";
                existingTrack.Callsign = "TR-" + existingTrack.Id.ToString()[^6..];
            }
            else if (existingTrack.Latitude < 30)
            {
                existingTrack.IffType = "Enemy";
                existingTrack.Country = "Greece";
                existingTrack.Callsign = "GR-" + existingTrack.Id.ToString()[^6..];
            }
            else if (existingTrack.Latitude >= 35 && existingTrack.Latitude <= 40)
            {
                existingTrack.IffType = "Neutral";
                existingTrack.Country = "Italy";
                existingTrack.Callsign = "IT-" + existingTrack.Id.ToString()[^6..];
            }
            else
            {
                existingTrack.IffType = "Unknown";
                existingTrack.Country = "Unknown";
                existingTrack.Callsign = "UNK-" + existingTrack.Id.ToString()[^6..];
            }

            existingTrack.IffUpdatedAt = DateTime.Now;

            // EntityFramework SaveChangesAsync kullan
            await dbContext.SaveChangesAsync();

            _logger.LogInformation($"IFF Analysis completed for track {existingTrack.Callsign} - {existingTrack.IffType}");

            return existingTrack;
        }

        public override void Dispose()
        {
            _consumer?.Close();
            _consumer?.Dispose();
            base.Dispose();
        }
    }
}
