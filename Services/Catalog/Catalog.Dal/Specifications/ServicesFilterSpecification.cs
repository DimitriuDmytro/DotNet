namespace Catalog.Dal.Specifications;

using Ardalis.Specification;
using Catalog.Domain.Entities;

/// <summary>
/// Специфікація фільтрації, пошуку, сортування та пагінації послуг (Критерій 14).
/// Виконується на рівні SQL через IQueryable ДО ToListAsync.
/// </summary>
public class ServicesFilterSpecification : Specification<Service>
{
    public ServicesFilterSpecification(
        string? search = null,
        long? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        string? sortDir = null,
        int page = 1,
        int pageSize = 20)
    {
        Query.Include(s => s.Category)
             .Include(s => s.Spec)
             .Include(s => s.Images);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            Query.Where(s => s.Name.ToLower().Contains(term));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            Query.Where(s => s.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue && minPrice.Value > 0)
        {
            Query.Where(s => s.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            Query.Where(s => s.Price <= maxPrice.Value);
        }

        // Безпечне сортування виключно з білого списку полів (Критерій 10)
        var isDescending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        switch (sortBy?.Trim().ToLowerInvariant())
        {
            case "price":
                if (isDescending) Query.OrderByDescending(s => s.Price);
                else Query.OrderBy(s => s.Price);
                break;

            case "duration":
            case "durationmin":
                if (isDescending) Query.OrderByDescending(s => s.DurationMin);
                else Query.OrderBy(s => s.DurationMin);
                break;

            case "createdat":
                if (isDescending) Query.OrderByDescending(s => s.CreatedAt);
                else Query.OrderBy(s => s.CreatedAt);
                break;

            case "name":
            default:
                if (isDescending) Query.OrderByDescending(s => s.Name);
                else Query.OrderBy(s => s.Name);
                break;
        }

        // Пагінація на рівні SQL (OFFSET / FETCH)
        var clampedPage = Math.Max(1, page);
        var clampedPageSize = Math.Clamp(pageSize, 1, 100);
        var skip = (clampedPage - 1) * clampedPageSize;

        Query.Skip(skip).Take(clampedPageSize);
    }
}
