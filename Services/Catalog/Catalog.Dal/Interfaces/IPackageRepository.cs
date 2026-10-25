namespace Catalog.Dal.Interfaces;

using Catalog.Domain.Entities;

/// <summary>
/// Специфічний репозиторій для комплексних пакетів послуг (Критерій 6).
/// Реалізує LINQ to Entities вибірку через проміжну сутність M:N (PackageService)
/// з фільтром по її власному полю AddedAt.
/// </summary>
public interface IPackageRepository : IGenericRepository<Package>
{
    /// <summary>
    /// Eager loading пакета разом зі складом його послуг.
    /// </summary>
    Task<Package?> GetWithServicesAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Критерій 6: LINQ to Entities через проміжну сутність M:N із фільтром по її власному полю (AddedAt >= since).
    /// Повертає колекцію Service зі складу пакета, які були додані до пакета починаючи з дати 'since'.
    /// </summary>
    Task<IReadOnlyList<Service>> GetRecentlyAddedServicesAsync(long packageId, DateTime since, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken cancellationToken = default);
}
