using Confluent.Kafka;
using KafkaProducerDemo.StandardizeModels;
using MySql.Data.MySqlClient;
using Npgsql;
using Serilog;
using System.Text.Json;

namespace KafkaProducerDemo.Services
{
  public class OrderPaymentService
  {
    private KafkaProducerService _kafkaProducerService;
    private readonly string _postgresConnectionString;
    private readonly string _postgresConnectionString2;
    private readonly string _postgresConnectionString3;
    private readonly string _mySqlConnectionString;
    private readonly string _mySqlConnectionString2;
    private readonly string _topic = "order_payment_topic";
    private readonly int _offsetStart;
    private readonly int _comparedBatchSize = 50000;
    public OrderPaymentService(IConfiguration configuration, KafkaProducerService kafkaProducerService, ILogger<OrderPaymentService> logger)
    {
      _kafkaProducerService = kafkaProducerService;
      _postgresConnectionString = configuration.GetSection("PostgreSql:ConnectionString").Value ?? throw new ArgumentNullException("PostgreSql:ConnectionString", "PostgreSQL connection string is not configured.");
      _postgresConnectionString2 = configuration.GetSection("PostgreSql:ConnectionString2").Value ?? throw new ArgumentNullException("PostgreSql:ConnectionString2", "PostgreSQL connection string 2 is not configured.");
      _postgresConnectionString3 = configuration.GetSection("PostgreSql:ConnectionString3").Value ?? throw new ArgumentNullException("PostgreSql:ConnectionString3", "PostgreSQL connection string 3 is not configured.");
      _mySqlConnectionString = configuration.GetSection("MySql:ConnectionString").Value ?? throw new ArgumentNullException("MySql:ConnectionString", "MySQL connection string is not configured.");
      _mySqlConnectionString2 = configuration.GetSection("MySql:ConnectionString2").Value ?? throw new ArgumentNullException("MySql:ConnectionString2", "MySQL connection string 2 is not configured.");
      _offsetStart = int.TryParse(Environment.GetEnvironmentVariable("OFFSET_START"), out var offset) ? offset : 0;
    }

    public async Task ProcessAndSendDataAsync()
    {
      const int batchSize = 500;
      var offset = _offsetStart;
      var totalRecords = 0;

      while (totalRecords < _comparedBatchSize)
      {
        var records = await GetOrderPaymentDataAsync(_postgresConnectionString, offset, batchSize);
        if (records.Count == 0) break;

        var tasks = records.Select(record =>
            Task.Run(() => _kafkaProducerService.SendToKafkaAsync(_topic, record.OrderId.ToString(), JsonSerializer.Serialize(record)))
          );

        await Task.WhenAll(tasks);
        Log.Information($"Processed {records.Count} records from PostgreSQL and sent to Kafka topic {_topic}.");
        totalRecords += records.Count;
        offset += batchSize;
      }
    }

    private async Task<List<OrderPaymentRecord>> GetOrderPaymentDataAsync(string connectionString, int offset, int batchSize)
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

      await using var connection = new NpgsqlConnection(connectionString);
      await connection.OpenAsync();
      var startTime = DateTime.UtcNow;
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
      var endTime = DateTime.UtcNow;
      Log.Information($"GetOrderPaymentDataAsync took {endTime - startTime} seconds to fetch {records.Count} records from PostgreSQL.");
      return records;
    }

    public async Task<List<OrderPaymentRecord>> GetListOrderPaymentPostgreSql()
    {
      return await GetOrderPaymentDataAsync(_postgresConnectionString, 0, _comparedBatchSize);
    }

    public async Task<List<OrderPaymentRecord>> GetListOrderPaymentPostgreSql2()
    {
      return await GetOrderPaymentDataAsync(_postgresConnectionString, 5000000, _comparedBatchSize);
    }

    public async Task<List<OrderPaymentRecord>> GetListOrderPaymentPostgreSqlCompared()
    {
      return await GetOrderPaymentDataAsync(_postgresConnectionString2, 0, _comparedBatchSize * 2);
    }

    public async Task<List<OrderPaymentRecord>> GetListOrderPaymentPostgreSqlCompared2()
    {
      return await GetOrderPaymentDataAsync(_postgresConnectionString3, 0, _comparedBatchSize * 2);
    }


    private async Task<List<OrderPaymentRecord>> GetOrderPaymentDataMySqlAsync(string connectionString, int offset, int batchSize)
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

      await using var connection = new MySqlConnection(connectionString);
      await connection.OpenAsync();

      var startTime = DateTime.UtcNow;
      await using var command = new MySqlCommand(query, connection);
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
      var endTime = DateTime.UtcNow;
      Log.Information($"GetOrderPaymentDataMySqlAsync took {endTime - startTime} seconds to fetch {records.Count} records from MySQL.");
      return records;
    }

    public async Task<List<OrderPaymentRecord>> GetListOrderPaymentMySqlCompared()
    {
      return await GetOrderPaymentDataMySqlAsync(_mySqlConnectionString, 0, _comparedBatchSize * 2);
    }

    public async Task<List<OrderPaymentRecord>> GetListOrderPaymentMySqlCompared2()
    {
      return await GetOrderPaymentDataMySqlAsync(_mySqlConnectionString2, 0, _comparedBatchSize * 2);
    }

    public (double percent, List<OrderPaymentRecord> listRecordNotFound) CalculateDifferencePercentage(List<OrderPaymentRecord> list1, List<OrderPaymentRecord> list2)
    {
      //Compare same orderId, same paymentId, same orderState, same paymentState is a valid same
      var sameCount = list1.Count(record1 => list2.Any(record2 =>
          record1.OrderId == record2.OrderId &&
          record1.PaymentId == record2.PaymentId &&
          record1.OrderState == record2.OrderState &&
          record1.PaymentState == record2.PaymentState));
      var list2Lookup = list2.ToDictionary(record => record.OrderId);
      var listRecordNotFound = list1.Where(record1 => !list2Lookup.ContainsKey(record1.OrderId)).ToList();
      var totalCount = list1.Count;
      if (totalCount == 0) return (0, new List<OrderPaymentRecord>()); // Avoid division by zero
      return ((double)sameCount / totalCount * 100, listRecordNotFound); // Calculate percentage of difference
    }
  }
}