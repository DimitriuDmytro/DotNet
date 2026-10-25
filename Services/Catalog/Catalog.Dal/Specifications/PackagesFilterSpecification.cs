namespace Catalog.Dal.Specifications;

using Ardalis.Specification;
using Catalog.Domain.Entities;

public class PackagesFilterSpecification : Specification<Package>
{
    public PackagesFilterSpecification(
        string? search = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int page = 1,
        int pageSize = 20)
    {
        Query.Include(p => p.PackageServices)
             .ThenInclude(ps => ps.Service);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            Query.Where(p => p.Name.Contains(term));
        }

        if (minPrice.HasValue && minPrice.Value > 0)
        {
            Query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            Query.Where(p => p.Price <= maxPrice.Value);
        }

        Query.OrderBy(p => p.Price).ThenBy(p => p.Id);

        var clampedPage = Math.Max(1, page);
        var clampedPageSize = Math.Clamp(pageSize, 1, 100);
        Query.Skip((clampedPage - 1) * clampedPageSize).Take(clampedPageSize);
    }
}

public class PackagesFilterCountSpecification : Specification<Package>
{
    public PackagesFilterCountSpecification(
        string? search = null,
        decimal? minPrice = null,
        decimal? maxPrice = null)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            Query.Where(p => p.Name.Contains(term));
        }

        if (minPrice.HasValue && minPrice.Value > 0)
        {
            Query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            Query.Where(p => p.Price <= maxPrice.Value);
        }
    }
}
