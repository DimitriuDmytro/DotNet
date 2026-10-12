namespace Bookings.Bll.Interfaces;

using Bookings.Bll.DTOs;

public interface ICatalogQueryService
{
    Task<IReadOnlyList<CatalogServiceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CatalogServiceDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task UpsertAsync(UpsertCatalogServiceDto dto, CancellationToken cancellationToken = default);
}
