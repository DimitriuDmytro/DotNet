namespace Catalog.Dal.Interfaces;

using Catalog.Domain.Entities;

/// <summary>
/// Специфічний репозиторій для категорій послуг (Критерій 6).
/// </summary>
public interface IServiceCategoryRepository : IGenericRepository<ServiceCategory>
{
    Task<ServiceCategory?> GetWithServicesAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken cancellationToken = default);
}
