namespace Bookings.Dal.Interfaces;

using Bookings.Domain.Models;

/// <summary>
/// Репозиторій локальної репліки довідника послуг (Services) у контексті Bookings.
/// </summary>
public interface ICatalogServiceRepository : IGenericRepository<CatalogService, long>
{
    Task<CatalogService?> GetByServiceIdAsync(long serviceId, CancellationToken cancellationToken = default);
    Task UpsertAsync(CatalogService service, CancellationToken cancellationToken = default);
}
