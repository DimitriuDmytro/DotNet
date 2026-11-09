namespace Reviews.Infrastructure.Persistence;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

/// <summary>
/// Контекст бази даних MongoDB (Критерій 10) з підтримкою OpenTelemetry DiagnosticSources.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbOptions> options, IConfiguration? configuration = null)
    {
        MongoMappings.Register();

        // Дозволяємо перевизначення рядка підключення через стандартний GetConnectionString("ReviewsDb")
        var connectionString = configuration?.GetConnectionString("ReviewsDb")
            ?? options.Value.ConnectionString;

        var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
        clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());

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
