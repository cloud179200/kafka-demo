using MongoDB.Bson;
using MongoDB.Driver;

namespace KafkaConsumerDemo.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<BsonDocument> _logsCollection;

        public MongoDbService(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDb:ConnectionString"];
            var databaseName = configuration["MongoDb:DatabaseName"];
            var collectionName = configuration["MongoDb:CollectionName"];

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _logsCollection = database.GetCollection<BsonDocument>(collectionName);
        }

        public async Task SaveLogAsync(string logMessage)
        {
            var document = new BsonDocument
            {
                { "LogMessage", logMessage },
                { "Timestamp", DateTime.UtcNow }
            };

            await _logsCollection.InsertOneAsync(document);
        }
    }
}
