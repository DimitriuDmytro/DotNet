namespace Reviews.Infrastructure.Persistence;

using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Interfaces;

/// <summary>
/// Реалізація відстеження оброблених подій у MongoDB (Критерій 9).
/// Використовує унікальний ідентифікатор _id в колекції processedEvents для перехоплення дублікатів (код 11000).
/// </summary>
public class MongoProcessedEventTracker : IProcessedEventTracker
{
    private readonly IMongoCollection<BsonDocument> _collection;

    public MongoProcessedEventTracker(MongoDbContext context)
    {
        _collection = context.Database.GetCollection<BsonDocument>("processedEvents");
    }

    public async Task<bool> TryTrackAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var doc = new BsonDocument
        {
            ["_id"] = eventId.ToString(),
            ["processedAt"] = DateTime.UtcNow
        };

        try
        {
            await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey || ex.WriteError.Code == 11000)
        {
            // Дублікат події -> повідомлення вже було оброблене
            return false;
        }
    }

    public async Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", eventId.ToString());
        return await _collection.Find(filter).AnyAsync(cancellationToken);
    }
}
