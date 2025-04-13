using Confluent.Kafka;
using KafkaProducerDemo.StandardizeModels;
using Npgsql;
using System.Text.Json;

namespace KafkaProducerDemo.Services
{
  public class OrderPaymentService
  {
    private readonly string _postgresConnectionString;
    private KafkaProducerService _kafkaProducerService;
    private readonly string _topic = "order_payment_topic";
    private readonly int _offsetStart;
    public OrderPaymentService(IConfiguration configuration, KafkaProducerService kafkaProducerService)
    {
      _kafkaProducerService = kafkaProducerService;
      _postgresConnectionString = configuration.GetSection("PostgreSql:ConnectionString").Value ?? throw new ArgumentNullException("PostgreSql:ConnectionString", "PostgreSQL connection string is not configured.");
      _offsetStart = int.TryParse(Environment.GetEnvironmentVariable("OFFSET_START"), out var offset) ? offset : 0;
    }

    public async Task ProcessAndSendDataAsync()
    {
      const int batchSize = 10000;
      var offset = _offsetStart;
      var totalRecords = 0;

      while (true)
      {
        var records = await GetOrderPaymentDataAsync(offset, batchSize);
        if (records.Count == 0) break;

        var tasks = records.Select(record =>
            Task.Run(() => _kafkaProducerService.SendToKafkaAsync(_topic, record.OrderId.ToString(), JsonSerializer.Serialize(record)))
          );

        await Task.WhenAll(tasks);
        Console.WriteLine($"Success count: {_kafkaProducerService.GetSuccessCount()}");
        Console.WriteLine($"Failed count: {_kafkaProducerService.GetFailedCount()}");
        totalRecords += records.Count;
        if (totalRecords >= 200000)
        {
          Console.WriteLine($"Processed {totalRecords} records. Stop here.");
          break;
        }
        offset += batchSize;
      }
    }

    private async Task<List<OrderPaymentRecord>> GetOrderPaymentDataAsync(int offset, int batchSize)
    {
      var records = new List<OrderPaymentRecord>();

      var query = @"
                SELECT 
                    o.order_id, o.customer_id, o.order_date, o.total_amount, o.state AS order_state,
                    p.payment_id, p.payment_date, p.payment_amount, p.payment_method, p.state AS payment_state
                FROM orders o
                LEFT JOIN payments p ON o.order_id = p.order_id
                ORDER BY o.order_id
                LIMIT @batchSize OFFSET @offset";

      await using var connection = new NpgsqlConnection(_postgresConnectionString);
      await connection.OpenAsync();

      await using var command = new NpgsqlCommand(query, connection);
      command.Parameters.AddWithValue("batchSize", batchSize);
      command.Parameters.AddWithValue("offset", offset);

      await using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        records.Add(new OrderPaymentRecord
        {
          OrderId = reader.GetInt64(0),
          CustomerId = reader.GetInt32(1),
          OrderDate = reader.GetDateTime(2),
          TotalAmount = reader.GetDecimal(3),
          OrderState = reader.GetString(4),
          PaymentId = reader.IsDBNull(5) ? null : reader.GetInt64(5),
          PaymentDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
          PaymentAmount = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
          PaymentMethod = reader.IsDBNull(8) ? null : reader.GetString(8),
          PaymentState = reader.IsDBNull(9) ? null : reader.GetString(9)
        });
      }
      return records;
    }
  }
}