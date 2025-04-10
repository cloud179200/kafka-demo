using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerDemo.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly MongoDbService _mongoDbService;
        private readonly string _bootstrapServers;
        private readonly string _groupId;
        private readonly string _topic;

        public KafkaConsumerService(ILogger<KafkaConsumerService> logger, MongoDbService mongoDbService, IConfiguration configuration)
        {
            _logger = logger;
            _mongoDbService = mongoDbService;
            _bootstrapServers = configuration["Kafka:BootstrapServers"];
            _groupId = configuration["Kafka:GroupId"];
            _topic = configuration["Kafka:Topic"];
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(_topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = consumer.Consume(stoppingToken);
                    var logMessage = $"Received message: {result.Message.Value} at {DateTime.UtcNow}";

                    _logger.LogInformation(logMessage);

                    // Save the log to MongoDB
                    await _mongoDbService.SaveLogAsync(logMessage);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consumer stopped.");
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}
