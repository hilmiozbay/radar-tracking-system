using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Drawing;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TrackLibrary;
using WebSocketService.DbContextClass;
using WebSocketService.Kafka;


namespace WebSocketService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DrawController : ControllerBase
    {
        private readonly ILogger<DrawController> _logger;
        private readonly AreaDbContext _aDbContext;
        private readonly IConfiguration _configuration;
        private readonly KafkaProducerService _kafkaProducer;

        public DrawController(ILogger<DrawController> logger, AreaDbContext aDbContext, IConfiguration configuration, KafkaProducerService kafkaProducer)
        {
            _logger = logger;
            _aDbContext = aDbContext;
            _configuration = configuration;
            _kafkaProducer = kafkaProducer;
        }

        [HttpGet]
        [Route("/ws/draw")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest){
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await HandleDrawMessage(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private async Task HandleDrawMessage(WebSocket webSocket)
        {
            // mesaj al +
            // mesajdan nesne uret +
            // db kaydet + 
            // kafkaya id gonder +

            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), HttpContext.RequestAborted);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                
                try
                {
                    var request = JsonSerializer.Deserialize<RectangleRequest>(message);

                    if (request != null)
                    {
                        var area = new Area
                        { 
                            MinLatitude = request.MinLat,
                            MaxLatitude = request.MaxLat,
                            MinLongitude = request.MinLng,
                            MaxLongitude = request.MaxLng,
                            CreatedAt = DateTime.UtcNow,
                            Name = "", 
                            IsActive = true
                        };
                        
                        _aDbContext.Areas.Add(area);
                        await _aDbContext.SaveChangesAsync();

                        await _kafkaProducer.NotifyTrackAsync(area.Id.ToString(), "draw_topic");
                    }

                }
                catch(Exception ex)
                {
                    _logger.LogInformation(ex.Message);
                }
            }
        }
    }

    public class RectangleRequest
    {
        public double MinLat { get; set; }
        public double MaxLat { get; set; }
        public double MinLng { get; set; }
        public double MaxLng { get; set; }
    }

    
}