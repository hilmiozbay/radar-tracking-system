using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net.WebSockets;
using System.Text;
using WebSocketService.DbContextClass;
using TrackLibrary;


namespace WebSocketService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateTopicController : ControllerBase
    {
        private readonly ILogger<UpdateTopicController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public UpdateTopicController(ILogger<UpdateTopicController> logger, ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/ws/update")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await StreamCreateData(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private async Task StreamCreateData(WebSocket webSocket)
        {
            // Kafka consumer konfigürasyonu
            var config = new ConsumerConfig
            {
                GroupId = "web-socket-update-fixed", // => grup id sabit değer olmalı
                BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false,
                SessionTimeoutMs = 30000,
                MaxPollIntervalMs = 300000
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

            consumer.Subscribe("update_track"); // IFF enriched tracks + RadarService updates

            var cancellationToken = HttpContext.RequestAborted;

            _logger.LogInformation("WebSocket UPDATE consumer başlatıldı - update_track topic'ini dinliyor");

            try
            {
                while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(1000));
                    if (consumeResult != null)
                    {
                        try
                        {
                            // Kafka mesajından TrackId'yi al (hem RadarService hem IFF Service'den)
                            var trackId = consumeResult.Message.Value;
                            _logger.LogInformation($"Kafka mesajı alındı: {trackId}");

                            if (!string.IsNullOrEmpty(trackId) && ObjectId.TryParse(trackId, out ObjectId objectId))
                            {
                                // MongoDB'den güncel track verisini çek - Cache bypass için farklı context
                                Track track = null;
                                using (var serviceScope = HttpContext.RequestServices.CreateScope())
                                {
                                    var freshContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                                    track = await freshContext.Tracks.FindAsync(new object[] { objectId }, cancellationToken);
                                }

                                if (track != null)
                                {
                                    Console.WriteLine(track.Latitude);
                                    Console.WriteLine(track.Longitude);
                                    var message = new
                                    {
                                        Topic = consumeResult.Topic,
                                        Data = new
                                        {
                                            Id = track.Id.ToString(),
                                            track.Latitude,
                                            track.Longitude,
                                            track.Altitude,
                                            track.Angle,
                                            track.Speed,
                                            track.Environment,
                                            track.LastUpdate,
                                            track.IffType,
                                            track.Country,
                                            track.Callsign,
                                            track.IffUpdatedAt
                                        },
                                        Timestamp = DateTime.UtcNow,
                                        Partition = consumeResult.Partition.Value,
                                        Offset = consumeResult.Offset.Value,
                                    };


                                    var messageJson = System.Text.Json.JsonSerializer.Serialize(message);
                                    var messageBytes = Encoding.UTF8.GetBytes(messageJson);

                                    // WebSocket durumunu kontrol et ve mesaj gönder
                                    if (webSocket.State == WebSocketState.Open)
                                    {
                                        await webSocket.SendAsync(
                                            new ArraySegment<byte>(messageBytes),
                                            WebSocketMessageType.Text,
                                            true,
                                            cancellationToken);

                                        _logger.LogInformation($"Track verisi gönderildi: {track.Id}");
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"WebSocket kapalı, mesaj gönderilemedi. Durum: {webSocket.State}");
                                        break;
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning($"Track bulunamadı: {trackId}");
                                }
                            }
                            else
                            {
                                _logger.LogWarning($"Geçersiz track ID: {trackId}");
                            }

                            consumer.Commit(consumeResult);
                        }
                        
                        catch (Exception ex)
                        {
                            _logger.LogError($"Mesaj işleme hatası: {ex.Message}");
                            
                        }
                    }
                    
                    
                    await Task.Delay(10, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Kafka streaming: {ex.Message}");
            }
            finally
            {
                try
                {
                    consumer.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Kafka consumer kapatılırken hata: {ex.Message}");
                }
                
                if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                {
                    try
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Kafka connection closed",
                            CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"WebSocket kapatılırken hata: {ex.Message}");
                    }
                }
            }
        }
    }
}