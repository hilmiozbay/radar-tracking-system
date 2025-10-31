using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net.WebSockets;
using System.Text;
using WebSocketService.DbContextClass;

namespace WebSocketService.Controllers
{
    //[Route("api/[controller]")]
    [ApiController]
    public class CreateTopicController : ControllerBase
    {
        private readonly ILogger<CreateTopicController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public CreateTopicController(ILogger<CreateTopicController> logger, IConfiguration configuration, ApplicationDbContext context)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        [HttpGet]
        [Route("/ws/create")]
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
            var config = new ConsumerConfig
            {
                GroupId = "web-socket-create-",
                BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false,
                SessionTimeoutMs = 30000,
                MaxPollIntervalMs = 300000
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

            
            consumer.Subscribe("new_track");

            var cancellationToken = HttpContext.RequestAborted;

            _logger.LogInformation("WebSocket consumer başlatıldı, topic: new_track (brand new tracks)");

            try
            {
                // Bağlantı kurulduğunda test mesajı gönder
                if (webSocket.State == WebSocketState.Open)
                {
                    var testMessage = new
                    {
                        Topic = "system",
                        Data = "WebSocket consumer başlatıldı ve Kafka'ya bağlanıyor...",
                        Timestamp = DateTime.UtcNow
                    };
                    var testJson = System.Text.Json.JsonSerializer.Serialize(testMessage);
                    var testBytes = Encoding.UTF8.GetBytes(testJson);

                    await webSocket.SendAsync(
                        new ArraySegment<byte>(testBytes),
                        WebSocketMessageType.Text,
                        true,
                        cancellationToken);
                }

                while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(1000));
                    if (consumeResult != null)
                    {
                        try
                        {
                            
                            var trackId = consumeResult.Message.Value;
                            _logger.LogInformation($"Kafka mesajı alındı - Topic: {consumeResult.Topic}, TrackId: {trackId}");

                            if (!string.IsNullOrEmpty(trackId) && ObjectId.TryParse(trackId, out ObjectId objectId))
                            {
                                // MongoDB'den güncel track verisini çek - EntityFramework FindAsync kullan
                                var track = await _context.Tracks.FindAsync(new object[] { objectId }, cancellationToken);

                                if (track != null)
                                {
                                    var message = new
                                    {
                                        consumeResult.Topic,
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

                                        _logger.LogInformation($"Track verisi gönderildi - ID: {track.Id}, Topic: {consumeResult.Topic}");
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"WebSocket kapalı, mesaj gönderilemedi. Durum: {webSocket.State}");
                                        break;
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning($"Track bulunamadı - ID: {trackId}, Topic: {consumeResult.Topic}");
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
                _logger.LogError($"WebSocket streaming hatası: {ex.Message}");
                
                if (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        var errorMessage = new
                        {
                            Topic = "error",
                            Data = $"Hata: {ex.Message}",
                            Timestamp = DateTime.UtcNow
                        };
                        var errorJson = System.Text.Json.JsonSerializer.Serialize(errorMessage);
                        var errorBytes = Encoding.UTF8.GetBytes(errorJson);

                        await webSocket.SendAsync(
                            new ArraySegment<byte>(errorBytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                    catch (Exception sendEx)
                    {
                        _logger.LogError($"Hata mesajı gönderilirken başka hata: {sendEx.Message}");
                    }
                }
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
                            "Connection closed",
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