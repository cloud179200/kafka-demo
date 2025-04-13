using Confluent.Kafka;
using KafkaProducerDemo.Models;
using KafkaProducerDemo.StandardizeModels;
using Npgsql;
using System.Text.Json;

namespace KafkaProducerDemo.Services
{
    public class KafkaProducerService
    {
        private readonly string _bootstrapServers;
        private readonly string _postgresConnectionString;

        public KafkaProducerService(IConfiguration configuration)
        {
            _bootstrapServers = configuration["Kafka:BootstrapServers"];
            _postgresConnectionString = configuration["PostgreSql:ConnectionString"];
        }

        public async Task SendMessageAsync(string key, string value)
        {
            var demoTopic = "demo";
            var config = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers
            };

            using var producer = new ProducerBuilder<string, string>(config).Build();
            try
            {
                var result = await producer.ProduceAsync(demoTopic, new Message<string, string>
                {
                    Key = key,
                    Value = value
                });

                Console.WriteLine($"Message sent to topic {result.Topic}, partition {result.Partition}, offset {result.Offset}");
            }
            catch (ProduceException<string, string> ex)
            {
                Console.WriteLine($"Error producing message: {ex.Error.Reason}");
                throw;
            }
        }

        public async Task SendUserDataToKafkaAsync()
        {
            var userTopic = "user_data";
            var config = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers
            };

            using var producer = new ProducerBuilder<string, string>(config).Build();

            try
            {
                // Step 1: Retrieve all records from the "user" table in PostgreSQL
                var users = await GetAllUsersFromPostgresAsync();

                // Step 2: Standardize and send each record to the Kafka topic
                foreach (var user in users)
                {
                    var standardizedUser = new StandardizeUserModel(user);
                    var message = JsonSerializer.Serialize(standardizedUser);

                    var result = await producer.ProduceAsync(userTopic, new Message<string, string>
                    {
                        Key = standardizedUser.Id.ToString(),
                        Value = message
                    });

                    Console.WriteLine($"Message sent to topic {result.Topic}, partition {result.Partition}, offset {result.Offset}");
                }
            }
            catch (ProduceException<string, string> ex)
            {
                Console.WriteLine($"Error producing message: {ex.Error.Reason}");
                throw;
            }
        }

        private async Task<List<User>> GetAllUsersFromPostgresAsync()
        {
            var users = new List<User>();

            await using var connection = new NpgsqlConnection(_postgresConnectionString);
            await connection.OpenAsync();

            var query = "SELECT id, name, email, created_at FROM user";
            await using var command = new NpgsqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    CreatedAt = reader.GetDateTime(3)
                });
            }

            return users;
        }
    }
}