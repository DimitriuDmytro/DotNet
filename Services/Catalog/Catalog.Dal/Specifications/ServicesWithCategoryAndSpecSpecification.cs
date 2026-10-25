namespace Catalog.Dal.Specifications;

using Ardalis.Specification;
using Catalog.Domain.Entities;

/// <summary>
/// Друга специфікація (Критерій 14): вибірка послуг із гарантією від minWarrantyMonths місяців.
/// Працює через той самий GenericRepository.ListAsync() без будь-яких змін у коді репозиторію.
/// </summary>
public class ServicesWithCategoryAndSpecSpecification : Specification<Service>
{
    public ServicesWithCategoryAndSpecSpecification(int minWarrantyMonths)
    {
        Query.Include(s => s.Category)
             .Include(s => s.Spec)
             .Where(s => s.Spec != null && s.Spec.WarrantyMonths >= minWarrantyMonths)
             .OrderByDescending(s => s.Spec!.WarrantyMonths);
    }
}
