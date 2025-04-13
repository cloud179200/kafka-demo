using Npgsql;

namespace KafkaConsumerDemo.Services
{
  public class PostgreSqlService
  {
    private readonly string _connectionString;

    public PostgreSqlService(IConfiguration configuration)
    {
      _connectionString = configuration.GetSection("PostgreSql:ConnectionString").Value;
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
  }
}