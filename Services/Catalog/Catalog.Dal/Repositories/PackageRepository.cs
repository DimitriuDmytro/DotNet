namespace Catalog.Dal.Repositories;

using Microsoft.EntityFrameworkCore;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;

public class PackageRepository : GenericRepository<Package>, IPackageRepository
{
    public PackageRepository(CatalogDbContext context) : base(context)
    {
    }

    public async Task<Package?> GetWithServicesAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.PackageServices)
                .ThenInclude(ps => ps.Service)
                    .ThenInclude(s => s!.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// Критерій 6.3: LINQ to Entities запит через проміжну сутність M:N (PackageServices)
    /// з фільтрацією по її власному полю AddedAt (AddedAt >= since) та проекцією у сутність Service.
    /// </summary>
    public async Task<IReadOnlyList<Service>> GetRecentlyAddedServicesAsync(
        long packageId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        return await Context.PackageServices
            .AsNoTracking()
            .Where(ps => ps.PackageId == packageId && ps.AddedAt >= since)
            .OrderBy(ps => ps.SortOrder)
            .Select(ps => ps.Service!)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        var query = DbSet.AsNoTracking().Where(p => p.Name == trimmed);
        if (excludeId.HasValue && excludeId.Value > 0)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
