using Confluent.Kafka;
using System.Text.Json;

namespace WebSocketService.Kafka
{
    public class KafkaProducerService
    {
        private readonly IProducer<Null, string> _producer;

        public KafkaProducerService(string bootstrapServers)
        {
            var config = new ProducerConfig { BootstrapServers = bootstrapServers };
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task PublishAsync<T>(string topic, T message)
        {
            var json = JsonSerializer.Serialize(message);
            await _producer.ProduceAsync(topic, new Message<Null, string> { Value = json });
        }

        // ID-based notification 
        public async Task NotifyTrackAsync(string trackId, string operation)
        {
            try
            {
                string topic = operation;
                var result = await _producer.ProduceAsync(topic, new Message<Null, string> { Value = trackId });
                Console.WriteLine($"Track {operation} ID notification gönderildi! Topic: {topic}, TrackId: {trackId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kafka {operation} notification hatası: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
}
