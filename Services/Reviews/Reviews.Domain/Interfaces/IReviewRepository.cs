namespace Reviews.Domain.Interfaces;

using MongoDB.Bson;
using Reviews.Domain.Entities;

/// <summary>
/// Інтерфейс репозиторію відгуків (Критерій 4).
/// Оголошений у доменному шарі та реалізується в інфраструктурі.
/// </summary>
public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(long serviceId, long clientId, CancellationToken cancellationToken = default);
    Task AddAsync(Review review, CancellationToken cancellationToken = default);
    Task UpdateAsync(Review review, int expectedVersion, CancellationToken cancellationToken = default);
    Task DeleteAsync(ObjectId id, CancellationToken cancellationToken = default);
}
