using KafkaConsumerDemo.StandardizeModels;
using Npgsql;

namespace KafkaConsumerDemo.Services
{
  public class PostgresSqlService
  {
    private readonly string _connectionString;

    public PostgresSqlService(IConfiguration configuration)
    {
      _connectionString = configuration.GetSection("POSTGRES_CONNECTION_STRING").Value ?? throw new ArgumentNullException("POSTGRES_CONNECTION_STRING", "PostgreSQL connection string is not configured.");
    }

    public async Task<List<string>> GetTableNamesAsync()
    {
      var tableNames = new List<string>();

      await using var connection = new NpgsqlConnection(_connectionString);
      await connection.OpenAsync();

      var query = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';";
      await using var command = new NpgsqlCommand(query, connection);
      await using var reader = await command.ExecuteReaderAsync();

      while (await reader.ReadAsync())
      {
        tableNames.Add(reader.GetString(0));
      }

      return tableNames;
    }

    public async Task SaveLogAsync(string logMessage)
    {
      await using var connection = new NpgsqlConnection(_connectionString);
      await connection.OpenAsync();

      var query = "INSERT INTO logs (message, created_at) VALUES (@message, @createdAt)";
      await using var command = new NpgsqlCommand(query, connection);
      command.Parameters.AddWithValue("message", logMessage);
      command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

      await command.ExecuteNonQueryAsync();
    }

    public async Task InsertOrderPaymentAsync(OrderPaymentRecord record)
    {
      await using var connection = new NpgsqlConnection(_connectionString);
      await connection.OpenAsync();

      // Insert into orders table
      var insertOrderQuery = @"
                INSERT INTO orders (order_id, customer_id, order_date, total_amount, state)
                VALUES (@orderId, @customerId, @orderDate, @totalAmount, @orderState)
                ON CONFLICT (order_id) DO NOTHING;";

      await using var orderCommand = new NpgsqlCommand(insertOrderQuery, connection);
      orderCommand.Parameters.AddWithValue("orderId", record.OrderId);
      orderCommand.Parameters.AddWithValue("customerId", record.CustomerId);
      orderCommand.Parameters.AddWithValue("orderDate", record.OrderDate);
      orderCommand.Parameters.AddWithValue("totalAmount", record.TotalAmount);
      orderCommand.Parameters.AddWithValue("orderState", record.OrderState);
      await orderCommand.ExecuteNonQueryAsync();

      // Insert into payments table (if payment exists)
      if (record.PaymentId.HasValue)
      {
        var insertPaymentQuery = @"
                    INSERT INTO payments (payment_id, order_id, payment_date, payment_amount, payment_method, state)
                    VALUES (@paymentId, @orderId, @paymentDate, @paymentAmount, @paymentMethod, @paymentState)
                    ON CONFLICT (payment_id) DO NOTHING;";

        await using var paymentCommand = new NpgsqlCommand(insertPaymentQuery, connection);
        paymentCommand.Parameters.AddWithValue("paymentId", record.PaymentId.Value);
        paymentCommand.Parameters.AddWithValue("orderId", record.OrderId);
        paymentCommand.Parameters.AddWithValue("paymentDate", record.PaymentDate ?? (object)DBNull.Value);
        paymentCommand.Parameters.AddWithValue("paymentAmount", record.PaymentAmount ?? (object)DBNull.Value);
        paymentCommand.Parameters.AddWithValue("paymentMethod", record.PaymentMethod ?? (object)DBNull.Value);
        paymentCommand.Parameters.AddWithValue("paymentState", record.PaymentState ?? (object)DBNull.Value);
        await paymentCommand.ExecuteNonQueryAsync();
      }
    }
    public async Task InsertOrderPaymentsAsync(List<OrderPaymentRecord> records)
    {
      if (records == null || records.Count == 0)
        return;

      await using var connection = new NpgsqlConnection(_connectionString);
      await connection.OpenAsync();

      await using var transaction = await connection.BeginTransactionAsync();

      try
      {
        // Insert all orders in a single batch
        var insertOrdersQuery = @"
            INSERT INTO orders (order_id, customer_id, order_date, total_amount, state)
            VALUES " + string.Join(", ", records.Select((_, i) => $"(@orderId{i}, @customerId{i}, @orderDate{i}, @totalAmount{i}, @orderState{i})")) + @"
            ON CONFLICT (order_id) DO NOTHING;";

        await using var orderCommand = new NpgsqlCommand(insertOrdersQuery, connection, transaction);
        for (var i = 0; i < records.Count; i++)
        {
          orderCommand.Parameters.AddWithValue($"orderId{i}", records[i].OrderId);
          orderCommand.Parameters.AddWithValue($"customerId{i}", records[i].CustomerId);
          orderCommand.Parameters.AddWithValue($"orderDate{i}", records[i].OrderDate);
          orderCommand.Parameters.AddWithValue($"totalAmount{i}", records[i].TotalAmount);
          orderCommand.Parameters.AddWithValue($"orderState{i}", records[i].OrderState);
        }
        await orderCommand.ExecuteNonQueryAsync();

        // Insert all payments in a single batch (only for records with payments)
        var paymentRecords = records.Where(r => r.PaymentId.HasValue).ToList();
        if (paymentRecords.Count > 0)
        {
          var insertPaymentsQuery = @"
                INSERT INTO payments (payment_id, order_id, payment_date, payment_amount, payment_method, state)
                VALUES " + string.Join(", ", paymentRecords.Select((_, i) => $"(@paymentId{i}, @orderId{i}, @paymentDate{i}, @paymentAmount{i}, @paymentMethod{i}, @paymentState{i})")) + @"
                ON CONFLICT (payment_id) DO NOTHING;";

          await using var paymentCommand = new NpgsqlCommand(insertPaymentsQuery, connection, transaction);
          for (var i = 0; i < paymentRecords.Count; i++)
          {
            paymentCommand.Parameters.AddWithValue($"paymentId{i}", paymentRecords[i].PaymentId.Value);
            paymentCommand.Parameters.AddWithValue($"orderId{i}", paymentRecords[i].OrderId);
            paymentCommand.Parameters.AddWithValue($"paymentDate{i}", paymentRecords[i].PaymentDate ?? (object)DBNull.Value);
            paymentCommand.Parameters.AddWithValue($"paymentAmount{i}", paymentRecords[i].PaymentAmount ?? (object)DBNull.Value);
            paymentCommand.Parameters.AddWithValue($"paymentMethod{i}", paymentRecords[i].PaymentMethod ?? (object)DBNull.Value);
            paymentCommand.Parameters.AddWithValue($"paymentState{i}", paymentRecords[i].PaymentState ?? (object)DBNull.Value);
          }
          await paymentCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
      }
      catch (Exception)
      {
        await transaction.RollbackAsync();
        throw;
      }
    }
  }
}