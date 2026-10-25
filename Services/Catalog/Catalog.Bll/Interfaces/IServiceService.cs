namespace Catalog.Bll.Interfaces;

using Catalog.Bll.DTOs;

public interface IServiceService
{
    Task<PagedResult<ServiceDto>> GetPagedAsync(ServiceQuery query, CancellationToken cancellationToken = default);
    Task<ServiceDetailDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ServiceDetailDto> GetByIdExplicitAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceDto>> GetWithMinWarrantyAsync(int minWarrantyMonths, CancellationToken cancellationToken = default);

    Task<ServiceDetailDto> CreateAsync(CreateServiceDto dto, CancellationToken cancellationToken = default);
    Task<ServiceDetailDto> UpdateAsync(long id, UpdateServiceDto dto, CancellationToken cancellationToken = default);
    Task<ServiceDetailDto> UpdatePriceAsync(long id, UpdatePriceDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<ServiceImageDto> AddImageAsync(long serviceId, AddServiceImageDto dto, CancellationToken cancellationToken = default);
    Task RemoveImageAsync(long serviceId, long imageId, CancellationToken cancellationToken = default);
    Task<ServiceSpecDto> UpsertSpecAsync(long serviceId, UpsertServiceSpecDto dto, CancellationToken cancellationToken = default);
}
