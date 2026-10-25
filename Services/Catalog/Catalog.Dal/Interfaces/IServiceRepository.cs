namespace Catalog.Dal.Interfaces;

using Catalog.Domain.Entities;

/// <summary>
/// Специфічний репозиторій для послуг (Критерій 6).
/// Реалізує Eager Loading, Explicit Loading та перевірку унікальності назви.
/// </summary>
public interface IServiceRepository : IGenericRepository<Service>
{
    /// <summary>
    /// Eager Loading: підвантаження сутності разом з категорією, специфікацією, фотографіями та пакетами ОДНИМ SQL-запитом (Include/ThenInclude).
    /// </summary>
    Task<Service?> GetWithDetailsAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicit Loading: підвантаження основної сутності окремим першим запитом,
    /// а специфікації та колекції фотографій — окремими наступними явними запитами (Entry.Reference / Entry.Collection).
    /// </summary>
    Task<Service?> GetWithExplicitDetailsAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdAsync(long id, CancellationToken cancellationToken = default);
}
