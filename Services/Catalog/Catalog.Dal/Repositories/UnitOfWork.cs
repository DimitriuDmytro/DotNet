namespace Catalog.Dal.Repositories;

using Microsoft.EntityFrameworkCore.Storage;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;

/// <summary>
/// Реалізація Unit of Work для атомарної координації репозиторіїв та транзакцій (Критерій 5).
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    private IServiceRepository? _services;
    private IPackageRepository? _packages;
    private IServiceCategoryRepository? _categories;
    private IGenericRepository<ServiceSpec>? _specs;
    private IGenericRepository<ServiceImage>? _images;
    private IGenericRepository<PackageService>? _packageServices;

    public UnitOfWork(CatalogDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IServiceRepository Services => _services ??= new ServiceRepository(_context);
    public IPackageRepository Packages => _packages ??= new PackageRepository(_context);
    public IServiceCategoryRepository Categories => _categories ??= new ServiceCategoryRepository(_context);
    public IGenericRepository<ServiceSpec> Specs => _specs ??= new GenericRepository<ServiceSpec>(_context);
    public IGenericRepository<ServiceImage> Images => _images ??= new GenericRepository<ServiceImage>(_context);
    public IGenericRepository<PackageService> PackageServices => _packageServices ??= new GenericRepository<PackageService>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
