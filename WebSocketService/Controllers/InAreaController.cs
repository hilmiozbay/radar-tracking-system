using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using WebSocketService.DbContextClass;

namespace WebSocketService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InAreaController : ControllerBase
    {

        private readonly ILogger<UpdateTopicController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public InAreaController(ILogger<UpdateTopicController> logger, ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;
        }
        
        [HttpGet]
        [Route("/ws/inside")]
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

        private async Task StreamCreateData(WebSocket webSocket){
            var config = new ConsumerConfig
            {
                GroupId = "geometry-service-two",
                BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false,
                SessionTimeoutMs = 30000,
                MaxPollIntervalMs = 300000
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe("alert_topic");

            var cancellationToken = new CancellationTokenSource().Token;

            try
            {
                while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
                {
                    var consumeResult = consumer.Consume(cancellationToken);
                    if (consumeResult != null && !string.IsNullOrEmpty(consumeResult.Message.Value))
                    {
                        var message = consumeResult.Message.Value;
                        var buffer = System.Text.Encoding.UTF8.GetBytes(message);
                        var segment = new ArraySegment<byte>(buffer);

                        await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while streaming Kafka messages to WebSocket.");
            }
            finally
            {
                consumer.Close();
            }
        }

    }
}