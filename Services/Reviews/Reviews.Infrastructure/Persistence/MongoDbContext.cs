namespace Reviews.Infrastructure.Persistence;

using Microsoft.Extensions.Options;
using MongoDB.Driver;

/// <summary>
/// Контекст бази даних MongoDB (Критерій 10).
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        MongoMappings.Register();

        var clientSettings = MongoClientSettings.FromConnectionString(options.Value.ConnectionString);
        var client = new MongoClient(clientSettings);
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public MongoDbContext(IMongoDatabase database)
    {
        MongoMappings.Register();
        _database = database;
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<Review> Reviews => _database.GetCollection<Review>("reviews");

    public IMongoCollection<Discussion> Discussions => _database.GetCollection<Discussion>("discussions");
}
