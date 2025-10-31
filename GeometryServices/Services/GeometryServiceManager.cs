
using Confluent.Kafka;
using GeometryServices.DbContextClass;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System.Text.Json;
using TrackLibrary;

namespace GeometryServices.Services
{
    public class GeometryServiceManager : BackgroundService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly AreaDbContext _aDbContext;
        private readonly KafkaProducerService _kafkaProducer;
        private IConsumer<Ignore, string>? _consumer;

        public GeometryServiceManager(ApplicationDbContext dbContext, AreaDbContext aDbContext, KafkaProducerService kafkaProducer)
        {
            _dbContext = dbContext;
            _aDbContext = aDbContext;
            _kafkaProducer = kafkaProducer;
            _consumer = null;
        }

        //private readonly ILogger _logger;


        private void InitializeConsumer()
        {
            if (_consumer == null)
            {
                var consumerConfig = new ConsumerConfig
                {
                    BootstrapServers = "localhost:9092",
                    GroupId = "geometry-service",
                    AutoCommitIntervalMs = 30000,
                    EnableAutoCommit = true,

                };
                _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
                _consumer.Subscribe(new[] { "draw_topic", "update_track","new_track" }); // new_track will be added

                //_logger.LogInformation("Draw topic is listened.");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    InitializeConsumer();
                    var result = _consumer.Consume(TimeSpan.FromMilliseconds(1000));

                    if (result != null)
                    {
                        string id = result.Message.Value;

                        switch (result.Topic)
                        {
                            case "draw_topic":
                            case "new_track" :   // kullanımı var mı 
                                await HandleAreaMessage(id, stoppingToken);
                                break;

                            case "update_track":
                                await HandleTrackMessage(id, stoppingToken);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Geometry Service Error: {ex.Message}");
                }
            }
        }

        private async Task HandleAreaMessage(string areaIdStr, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(areaIdStr, out ObjectId areaId)) return;

            var area = await _aDbContext.Areas.FindAsync(new object[] { areaId }, cancellationToken);

            if (area == null) return;

            
            var tracks = await _dbContext.Tracks.ToListAsync(cancellationToken);

            foreach (var track in tracks)
            {
                if (IsTrackInArea(track, area))
                {
                    await SendAlert(track, area);
                }
            }
        }

        private async Task HandleTrackMessage(string trackIdStr, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(trackIdStr, out ObjectId trackId))
            {
                return;
            }
            var track = await _dbContext.Tracks.FindAsync(new object[] { trackId }, cancellationToken);
            
            if (track != null)
            {
                track.LastUpdate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }


        private bool IsTrackInArea(Track track, Area area)
        {
            return track.Latitude >= area.MinLatitude &&
                   track.Latitude <= area.MaxLatitude &&
                   track.Longitude >= area.MinLongitude &&
                   track.Longitude <= area.MaxLongitude;
        }

        private async Task SendAlert(Track track, Area area)
        {
            var isInside = IsTrackInArea(track, area);
            try
            {
                var alertMessage = new
                {
                    TrackId = track.Id.ToString(),
                    AreaId = area.Id.ToString(),
                    AreaName = area.Name,
                    AlertType = isInside ? "ZONE_ENTRY" : "ZONE_EXIT",
                    Inside = isInside,
                    Timestamp = DateTime.UtcNow,
                    Position = new
                    {
                        Latitude = track.Latitude,
                        Longitude = track.Longitude
                    },
                    Message = isInside
                        ? $"Track {track.Id} entered area {area.Name}"
                        : $"Track {track.Id} exited area {area.Name}"
                };

                await _kafkaProducer.PublishAsync("alert_topic", alertMessage);

            }
            
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending alert: {ex.Message}");
            }
        }
    }
}