namespace Catalog.Dal.Repositories;

using Microsoft.EntityFrameworkCore;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;

public class ServiceRepository : GenericRepository<Service>, IServiceRepository
{
    public ServiceRepository(CatalogDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Критерій 6.1 (Eager Loading): підвантаження категорій, специфікації, фото та пакетів ОДНИМ запитом через Include/ThenInclude.
    /// </summary>
    public async Task<Service?> GetWithDetailsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.Category)
            .Include(s => s.Spec)
            .Include(s => s.Images)
            .Include(s => s.PackageServices)
                .ThenInclude(ps => ps.Package)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <summary>
    /// Критерій 6.2 (Explicit Loading): підвантаження сутності окремим першим запитом,
    /// а зв'язаних даних (Reference Spec та Collection Images) — окремими явними запитами через Entry().LoadAsync().
    /// </summary>
    public async Task<Service?> GetWithExplicitDetailsAsync(long id, CancellationToken cancellationToken = default)
    {
        // 1-й запит: завантажуємо лише сутність Service та її Category
        var service = await DbSet
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (service == null)
        {
            return null;
        }

        // 2-й запит: окремо явно завантажуємо 1:1 специфікацію через Reference().LoadAsync
        await Context.Entry(service)
            .Reference(s => s.Spec)
            .LoadAsync(cancellationToken);

        // 3-й запит: окремо явно завантажуємо 1:N колекцію фотографій через Collection().Query().LoadAsync
        await Context.Entry(service)
            .Collection(s => s.Images)
            .Query()
            .OrderByDescending(i => i.IsMain)
            .LoadAsync(cancellationToken);

        return service;
    }

    public async Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        var query = DbSet.AsNoTracking().Where(s => s.Name == trimmed);
        if (excludeId.HasValue && excludeId.Value > 0)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().AnyAsync(s => s.Id == id, cancellationToken);
    }
}
