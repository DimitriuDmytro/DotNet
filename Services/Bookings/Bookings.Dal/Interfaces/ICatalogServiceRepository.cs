namespace Bookings.Dal.Interfaces;

using Bookings.Domain.Models;

/// <summary>
/// Репозиторій локальної репліки довідника послуг (Services) у контексті Bookings.
/// Успадковує IGenericRepository<CatalogService> без дублювання базового GetByIdAsync.
/// </summary>
public interface ICatalogServiceRepository : IGenericRepository<CatalogService>
{
    /// <summary>
    /// Пакетне завантаження послуг за переліком ідентифікаторів (за 1 запит WHERE Id IN @Ids).
    /// </summary>
    Task<IReadOnlyList<CatalogService>> GetByIdsAsync(IEnumerable<long> serviceIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ідемпотентне додавання або оновлення послуги в локальній Read-Model копії (MERGE).
    /// </summary>
    Task UpsertAsync(CatalogService service, CancellationToken cancellationToken = default);

    /// <summary>
    /// Оновлення ціни репліки послуги з контролем черговості за OccurredAt (Критерій 10).
    /// </summary>
    Task<bool> UpdatePriceAsync(long serviceId, decimal newPrice, DateTime occurredAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Оновлення назви репліки послуги з контролем черговості за OccurredAt (Критерій 10).
    /// </summary>
    Task<bool> UpdateNameAsync(long serviceId, string newName, DateTime occurredAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Повне додавання або оновлення репліки з контролем черговості за OccurredAt (Критерій 10).
    /// </summary>
    Task<bool> UpsertReplicaAsync(long serviceId, string name, decimal price, int durationMin, DateTime occurredAt, CancellationToken cancellationToken = default);
}
