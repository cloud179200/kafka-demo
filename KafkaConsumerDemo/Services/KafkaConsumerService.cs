using Confluent.Kafka;
using Serilog;

namespace KafkaConsumerDemo.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly PostgresSqlService _postgreSqlService;
        private readonly ConsumerConfig _consumerConfig;
        private readonly string _topic;

        public KafkaConsumerService(
            PostgresSqlService postgreSqlService,
            IConfiguration configuration)
        {
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
                        Log.Information($"Partitions assigned: [{string.Join(", ", partitions)}]"))
                    .SetPartitionsRevokedHandler((c, partitions) =>
                        Log.Information($"Partitions revoked: [{string.Join(", ", partitions)}]"))
                    .Build();

                consumer.Subscribe(_topic);
                Log.Information($"Subscribed to topic: {_topic}");

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
                                Log.Information(logMessage);

                                // Save the log message to PostgreSQL
                                await _postgreSqlService.SaveLogAsync(logMessage);
                            }
                        }
                        catch (ConsumeException ex)
                        {
                            Log.Error(ex, $"Error occurred while consuming message: {ex.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Log.Information("Consumer operation canceled.");
                }
                catch (KafkaException ex)
                {
                    Log.Error(ex, $"Kafka error occurred: {ex.Error.Reason}");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An unexpected error occurred.");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                finally
                {
                    consumer.Close();
                    Log.Information("Consumer closed.");
                }
            }
        }
    }
}