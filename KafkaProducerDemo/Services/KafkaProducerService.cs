using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace KafkaProducerDemo.Services
{
    public class KafkaProducerService
    {
        private readonly string _bootstrapServers;
        private readonly string _topic;

        public KafkaProducerService(IConfiguration configuration)
        {
            _bootstrapServers = configuration["Kafka:BootstrapServers"];
            _topic = configuration["Kafka:Topic"];
        }

        public async Task SendMessageAsync(string key, string value)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers
            };

            using var producer = new ProducerBuilder<string, string>(config).Build();
            try
            {
                var result = await producer.ProduceAsync(_topic, new Message<string, string>
                {
                    Key = key,
                    Value = value
                });

                Console.WriteLine($"Message sent to topic {result.Topic}, partition {result.Partition}, offset {result.Offset}");
            }
            catch (ProduceException<string, string> ex)
            {
                Console.WriteLine($"Error producing message: {ex.Error.Reason}");
                throw;
            }
        }
    }
}
