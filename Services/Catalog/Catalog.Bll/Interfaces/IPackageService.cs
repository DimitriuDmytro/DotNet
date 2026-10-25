namespace Catalog.Bll.Interfaces;

using Catalog.Bll.DTOs;

public interface IPackageService
{
    Task<PagedResult<PackageDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? search = null, decimal? minPrice = null, decimal? maxPrice = null, CancellationToken cancellationToken = default);
    Task<PackageDetailDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceDto>> GetRecentlyAddedServicesAsync(long packageId, DateTime since, CancellationToken cancellationToken = default);

    Task<PackageDetailDto> CreateAsync(CreatePackageDto dto, CancellationToken cancellationToken = default);
    Task<PackageDetailDto> UpdateAsync(long id, UpdatePackageDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<PackageDetailDto> AddServiceAsync(long packageId, AddServiceToPackageDto dto, CancellationToken cancellationToken = default);
    Task<PackageDetailDto> RemoveServiceAsync(long packageId, long serviceId, CancellationToken cancellationToken = default);
}
