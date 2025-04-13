using KafkaConsumerDemo.StandardizeModels;
using MySql.Data.MySqlClient;

namespace KafkaConsumerDemo.Services
{
  public class MySqlService
  {
    private readonly string _connectionString;

    public MySqlService(IConfiguration configuration)
    {
      _connectionString = configuration.GetSection("MYSQL_CONNECTION_STRING").Value ?? throw new ArgumentNullException("MYSQL_CONNECTION_STRING", "MySQL connection string is not configured.");
    }

    public async Task InsertOrderPaymentAsync(OrderPaymentRecord record)
    {
      await using var connection = new MySqlConnection(_connectionString);
      await connection.OpenAsync();

      // Insert into orders table
      var insertOrderQuery = @"
                INSERT INTO orders (order_id, customer_id, order_date, total_amount, state)
                VALUES (@orderId, @customerId, @orderDate, @totalAmount, @orderState)
                ON DUPLICATE KEY UPDATE order_id = order_id;";

      await using var orderCommand = new MySqlCommand(insertOrderQuery, connection);
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
                    ON DUPLICATE KEY UPDATE payment_id = payment_id;";

        await using var paymentCommand = new MySqlCommand(insertPaymentQuery, connection);
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

      await using var connection = new MySqlConnection(_connectionString);
      await connection.OpenAsync();

      await using var transaction = await connection.BeginTransactionAsync();

      try
      {
        // Insert all orders in a single batch
        var insertOrdersQuery = @"
            INSERT INTO orders (order_id, customer_id, order_date, total_amount, state)
            VALUES " + string.Join(", ", records.Select((_, i) => $"(@orderId{i}, @customerId{i}, @orderDate{i}, @totalAmount{i}, @orderState{i})")) + @"
            ON DUPLICATE KEY UPDATE 
                customer_id = VALUES(customer_id),
                order_date = VALUES(order_date),
                total_amount = VALUES(total_amount),
                state = VALUES(state);";

        await using var orderCommand = new MySqlCommand(insertOrdersQuery, connection, transaction);
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
                ON DUPLICATE KEY UPDATE 
                    order_id = VALUES(order_id),
                    payment_date = VALUES(payment_date),
                    payment_amount = VALUES(payment_amount),
                    payment_method = VALUES(payment_method),
                    state = VALUES(state);";

          await using var paymentCommand = new MySqlCommand(insertPaymentsQuery, connection, transaction);
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