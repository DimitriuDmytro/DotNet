namespace Reviews.Infrastructure.Persistence;

using MongoDB.Bson;
using MongoDB.Driver;

/// <summary>
/// Репозиторій відгуків з підтримкою оптимістичного блокування (Критерій 4, 11).
/// Працює через IMongoCollection сутностей Review.
/// </summary>
public class ReviewRepository : IReviewRepository
{
    private readonly MongoDbContext _context;

    public ReviewRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(long serviceId, long authorClientId, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .Find(r => r.ServiceId == serviceId && r.Author.ClientId == authorClientId)
            .AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Review review, CancellationToken cancellationToken = default)
    {
        await _context.Reviews.InsertOneAsync(review, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Review review, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.Id, review.Id),
            Builders<Review>.Filter.Eq(r => r.Version, expectedVersion));

        review.BumpVersion();

        var result = await _context.Reviews.ReplaceOneAsync(filter, review, cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            var exists = await _context.Reviews
                .Find(r => r.Id == review.Id)
                .AnyAsync(cancellationToken);

            if (!exists)
            {
                throw new NotFoundException($"Відгук з ID '{review.Id}' не знайдено.");
            }

            throw new ConflictException(
                $"Конфлікт паралельного оновлення для відгуку {review.Id}. " +
                $"Очікувана версія: {expectedVersion}, проте документ у базі було оновлено іншим запитом.");
        }
    }

    public async Task DeleteAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var result = await _context.Reviews.DeleteOneAsync(r => r.Id == id, cancellationToken);

        if (result.DeletedCount == 0)
        {
            throw new NotFoundException($"Відгук з ID '{id}' не знайдено.");
        }
    }

    public async Task<long> UpdateServiceNameAsync(long serviceId, string newName, DateTime occurredAt, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.ServiceId, serviceId),
            Builders<Review>.Filter.Or(
                Builders<Review>.Filter.Eq(r => r.UpdatedAt, null),
                Builders<Review>.Filter.Lte(r => r.UpdatedAt, occurredAt)
            )
        );

        var update = Builders<Review>.Update
            .Set(r => r.ServiceName, newName.Trim())
            .Set(r => r.UpdatedAt, occurredAt);

        var result = await _context.Reviews.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount;
    }

    public async Task<long> UpdateWorkReportsServiceNameAsync(long serviceId, string newName, DateTime occurredAt, CancellationToken cancellationToken = default)
    {
        var workReports = _context.Database.GetCollection<BsonDocument>("workReports");
        var filter = Builders<BsonDocument>.Filter.Eq("serviceId", serviceId);
        var update = Builders<BsonDocument>.Update.Set("serviceName", newName.Trim());
        var result = await workReports.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount;
    }
}
