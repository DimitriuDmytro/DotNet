namespace Catalog.Bll.Interfaces;

using Catalog.Bll.DTOs;

public interface IServiceCategoryService
{
    Task<IReadOnlyList<ServiceCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceCategoryDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ServiceCategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<ServiceCategoryDto> UpdateAsync(long id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
}
