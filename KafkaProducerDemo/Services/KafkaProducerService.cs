using Confluent.Kafka;
using KafkaProducerDemo.StandardizeModels;
using Npgsql;
using Serilog;
using System.Text.Json;

namespace KafkaProducerDemo.Services
{
    public class KafkaProducerService
    {
        private readonly string _bootstrapServers;
        private int _successCount = 0;
        private int _failedCount = 0;
        public KafkaProducerService(IConfiguration configuration)
        {
            _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? throw new ArgumentNullException("Kafka:BootstrapServers", "Kafka bootstrap servers are not configured.");
        }

        public async Task SendToKafkaAsync(string topic, string key, string value)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers,
                Acks = Acks.All,
                MessageSendMaxRetries = 5,
                RetryBackoffMs = 1000
            };

            try
            {
                using var producer = new ProducerBuilder<string, string>(config).Build();
                var result = await producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = key,
                    Value = value
                });
                producer.Flush(TimeSpan.FromSeconds(10));
                _successCount++;
                Log.Information($"Message sent to topic {result.Topic}, partition {result.Partition}, offset {result.Offset}");
            }
            catch (Exception ex)
            {
                _failedCount++;
                Log.Error($"Failed to send message to Kafka: {ex.Message}");
            }
        }

        public int GetSuccessCount()
        {
            return _successCount;
        }

        public int GetFailedCount()
        {
            return _failedCount;
        }
    }
}