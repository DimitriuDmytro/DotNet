namespace Catalog.Dal.Specifications;

using Ardalis.Specification;
using Catalog.Domain.Entities;

/// <summary>
/// Специфікація для точного підрахунку кількості записів (TotalCount) за тими самими фільтрами без Skip/Take.
/// </summary>
public class ServicesFilterCountSpecification : Specification<Service>
{
    public ServicesFilterCountSpecification(
        string? search = null,
        long? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null)
    {
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
    }
}
