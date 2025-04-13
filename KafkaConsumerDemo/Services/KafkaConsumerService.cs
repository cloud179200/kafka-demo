using Confluent.Kafka;

namespace KafkaConsumerDemo.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly PostgresSqlService _postgreSqlService;
        private readonly ConsumerConfig _consumerConfig;
        private readonly string _topic;

        public KafkaConsumerService(
            ILogger<KafkaConsumerService> logger,
            PostgresSqlService postgreSqlService,
            IConfiguration configuration)
        {
            _logger = logger;
            _postgreSqlService = postgreSqlService;
            _topic = configuration["Kafka:Topic"] ?? throw new ArgumentNullException("Kafka:Topic");
            _consumerConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? throw new ArgumentNullException("Kafka:BootstrapServers"),
                GroupId = configuration["Kafka:GroupId"] ?? throw new ArgumentNullException("Kafka:GroupId"),
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var consumer = new ConsumerBuilder<Ignore, string>(_consumerConfig)
                    .SetPartitionsAssignedHandler((c, partitions) =>
                        _logger.LogInformation($"Partitions assigned: [{string.Join(", ", partitions)}]"))
                    .SetPartitionsRevokedHandler((c, partitions) =>
                        _logger.LogWarning("Partitions revoked."))
                    .Build();

                consumer.Subscribe(_topic);
                _logger.LogInformation("Subscribed to topic: {Topic}", _topic);

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var result = consumer.Consume(stoppingToken);
                            var message = result.Message?.Value;

                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                var logMessage = $"Received message: {message} at {DateTime.UtcNow}";
                                _logger.LogInformation(logMessage);

                                // Save the log message to PostgreSQL
                                await _postgreSqlService.SaveLogAsync(logMessage);
                            }
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError(ex, $"Consume error: {ex.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Consumer cancellation requested.");
                }
                catch (KafkaException ex)
                {
                    _logger.LogError(ex, "Kafka error occurred. Retrying in 5 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred. Retrying in 5 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                finally
                {
                    consumer.Close();
                    _logger.LogInformation("Consumer closed.");
                }
            }
        }
    }
}