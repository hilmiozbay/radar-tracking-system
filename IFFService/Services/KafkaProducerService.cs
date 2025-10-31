using Confluent.Kafka;
using System.Text.Json;

namespace IFFService.Services
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
            try
            {
                var json = JsonSerializer.Serialize(message);
                var messageObj = new Message<Null, string> { Value = json };
                
                var result = await _producer.ProduceAsync(topic, messageObj);
                Console.WriteLine($"Event published to {topic}: {result.TopicPartitionOffset}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Event publish hatası: {ex.Message}");
            }
        }

        public async Task PublishEventAsync<T>(string topic, T eventData)
        {
            await PublishAsync(topic, eventData);
        }

            // ID-based notification 
    public async Task NotifyTrackAsync(string trackId, string operation)
    {
        try
        {
            // Topic'i operasyon tipine göre belirle
            string topic = operation;

            // Sadece TrackId gönder - basit!
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
