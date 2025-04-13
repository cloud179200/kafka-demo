using Npgsql;

namespace KafkaProducerDemo.Services
{
  public class PostgreSqlService
  {
    private readonly string _connectionString;

    public PostgreSqlService(IConfiguration configuration)
    {
      _connectionString = configuration.GetSection("PostgreSql:ConnectionString").Value ?? throw new ArgumentNullException("PostgreSql:ConnectionString", "PostgreSQL connection string is not configured.");
    }

    public async Task<List<string>> GetTableNamesAsync()
    {
      var tableNames = new List<string>();

      await using var connection = new NpgsqlConnection(_connectionString);
      await connection.OpenAsync();

      var query = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'db1';";
      await using var command = new NpgsqlCommand(query, connection);
      await using var reader = await command.ExecuteReaderAsync();

      while (await reader.ReadAsync())
      {
        tableNames.Add(reader.GetString(0));
      }

      return tableNames;
    }
  }
}