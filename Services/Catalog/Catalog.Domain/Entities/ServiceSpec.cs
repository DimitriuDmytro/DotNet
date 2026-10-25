namespace Catalog.Domain.Entities;

using Catalog.Domain.Exceptions;

/// <summary>
/// Технічна специфікація послуги (матеріали, гарантійний термін, детальний опис).
/// Зв'язок 1:1 із сутністю Service. ServiceId є PK і водночас FK.
/// </summary>
public class ServiceSpec
{
    public long ServiceId { get; private set; }
    public string Materials { get; private set; } = string.Empty;
    public int WarrantyMonths { get; private set; }
    public string Description { get; private set; } = string.Empty;

    public Service? Service { get; private set; }

    // Приватний конструктор для EF Core
    private ServiceSpec()
    {
    }

    public ServiceSpec(long serviceId, string materials, int warrantyMonths, string description)
    {
        ServiceId = serviceId;
        Update(materials, warrantyMonths, description);
    }

    public void Update(string materials, int warrantyMonths, string description)
    {
        if (string.IsNullOrWhiteSpace(materials))
        {
            throw new DomainException("Перелік матеріалів специфікації є обов'язковим.");
        }

        if (warrantyMonths < 0)
        {
            throw new DomainException("Гарантійний термін (у місяцях) не може бути від'ємним.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Технологічний опис послуги не може бути порожнім.");
        }

        Materials = materials.Trim();
        WarrantyMonths = warrantyMonths;
        Description = description.Trim();
    }
}
