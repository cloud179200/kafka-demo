using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using KafkaConsumerDemo.StandardizeModels;
using Serilog;

namespace KafkaConsumerDemo.Services
{
  public class KafkaOrderPaymentConsumerService : BackgroundService
  {
    private readonly ConsumerConfig _consumerConfig;
    private readonly PostgresSqlService _postgreSqlService;
    private readonly MySqlService _mySqlService;
    private readonly ConcurrentBag<OrderPaymentRecord> _batchRecords = new();
    private readonly SemaphoreSlim _batchSemaphore = new(1, 1);
    private readonly string _topic;
    private readonly int _batchSize = 1000;
    private readonly TimeSpan _batchInterval = TimeSpan.FromSeconds(30);

    public KafkaOrderPaymentConsumerService(
        IConfiguration configuration,
        PostgresSqlService postgreSqlService,
        MySqlService mySqlService)
    {
      _topic = configuration["Kafka:OrderPaymentTopic"] ?? throw new ArgumentNullException("Kafka:OrderPaymentTopic");
      _consumerConfig = new ConsumerConfig
      {
        BootstrapServers = configuration["Kafka:BootstrapServers"] ?? throw new ArgumentNullException("Kafka:BootstrapServers"),
        GroupId = configuration["KAFKA_GROUP_ID"] ?? throw new ArgumentNullException("KAFKA_GROUP_ID"),
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false
      };
      _postgreSqlService = postgreSqlService ?? throw new ArgumentNullException(nameof(postgreSqlService));
      _mySqlService = mySqlService ?? throw new ArgumentNullException(nameof(mySqlService));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      var batchTask = Task.Run(() => ProcessBatchAsync(stoppingToken), stoppingToken);

      while (!stoppingToken.IsCancellationRequested)
      {
        using var consumer = new ConsumerBuilder<Ignore, string>(_consumerConfig).Build();
        consumer.Subscribe(_topic);

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
                var record = JsonSerializer.Deserialize<OrderPaymentRecord>(message);

                if (record != null)
                {
                  _batchRecords.Add(record);
                  consumer.Commit(result);
                  Log.Information("Message consumed: {OrderId}", record.OrderId);
                }
              }
            }
            catch (Exception ex)
            {
              Log.Error(ex, "Error occurred while consuming message: {Message}", ex.Message);
            }
          }
        }
        catch (Exception ex)
        {
          Log.Error(ex, "An error occurred in the Kafka consumer loop: {Message}", ex.Message);
        }
        finally
        {
          consumer.Close();
          Log.Information("Consumer closed.");
        }
      }

      await batchTask;
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          // Wait for either the batch interval or the batch size to be reached
          var delayTask = Task.Delay(_batchInterval, stoppingToken);
          while (_batchRecords.Count < _batchSize && !delayTask.IsCompleted)
          {
            await Task.WhenAny(delayTask, Task.Delay(100, stoppingToken)); // Check every 100ms
          }

          if (_batchRecords.Count > 0)
          {
            await _batchSemaphore.WaitAsync(stoppingToken);

            try
            {
              var recordsToProcess = _batchRecords.ToList();
              _batchRecords.Clear();

              if (recordsToProcess.Count > 0)
              {
                Log.Information("Processing batch of {Count} records.", recordsToProcess.Count);

                // Insert into PostgreSQL
                await _postgreSqlService.InsertOrderPaymentsAsync(recordsToProcess);

                // Insert into MySQL
                await _mySqlService.InsertOrderPaymentsAsync(recordsToProcess);

                Log.Information("Batch processed successfully.");
              }
            }
            finally
            {
              _batchSemaphore.Release();
            }
          }
        }
        catch (Exception ex)
        {
          Log.Error(ex, "An error occurred while processing the batch: {Message}", ex.Message);
        }
      }
    }
  }
}