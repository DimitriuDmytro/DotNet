namespace Catalog.Domain.Entities;

using Catalog.Domain.Exceptions;

/// <summary>
/// Явна проміжна сутність M:N зв'язку між Packages та Services.
/// Має складений первинний ключ (PackageId, ServiceId) та власні поля: SortOrder та AddedAt.
/// </summary>
public class PackageService
{
    public long PackageId { get; private set; }
    public long ServiceId { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime AddedAt { get; private set; }

    public Package? Package { get; private set; }
    public Service? Service { get; private set; }

    // Приватний конструктор для EF Core
    private PackageService()
    {
    }

    public PackageService(long packageId, long serviceId, int sortOrder, DateTime? addedAt = null)
    {
        PackageId = packageId;
        ServiceId = serviceId;
        SetSortOrder(sortOrder);
        AddedAt = addedAt ?? DateTime.UtcNow;
    }

    public void UpdateSortOrder(int sortOrder)
    {
        SetSortOrder(sortOrder);
    }

    private void SetSortOrder(int sortOrder)
    {
        if (sortOrder <= 0)
        {
            throw new DomainException("Порядковий номер послуги у пакеті (SortOrder) повинен бути більшим за 0.");
        }

        SortOrder = sortOrder;
    }
}
