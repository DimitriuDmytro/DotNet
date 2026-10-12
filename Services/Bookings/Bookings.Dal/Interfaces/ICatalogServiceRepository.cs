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
}
