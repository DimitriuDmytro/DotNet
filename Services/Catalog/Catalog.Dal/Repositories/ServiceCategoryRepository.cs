namespace Catalog.Dal.Repositories;

using Microsoft.EntityFrameworkCore;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;

public class ServiceCategoryRepository : GenericRepository<ServiceCategory>, IServiceCategoryRepository
{
    public ServiceCategoryRepository(CatalogDbContext context) : base(context)
    {
    }

    public async Task<ServiceCategory?> GetWithServicesAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        var query = DbSet.AsNoTracking().Where(c => c.Name == trimmed);
        if (excludeId.HasValue && excludeId.Value > 0)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
