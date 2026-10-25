namespace Catalog.Dal.Interfaces;

using Catalog.Domain.Entities;

/// <summary>
/// Інтерфейс Unit of Work для атомарної фіксації змін через єдиний SaveChangesAsync (Критерій 5).
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IServiceRepository Services { get; }
    IPackageRepository Packages { get; }
    IServiceCategoryRepository Categories { get; }
    IGenericRepository<ServiceSpec> Specs { get; }
    IGenericRepository<ServiceImage> Images { get; }
    IGenericRepository<PackageService> PackageServices { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
