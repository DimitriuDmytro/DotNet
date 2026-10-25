namespace Catalog.Domain.Entities;

using Catalog.Domain.Exceptions;

/// <summary>
/// Послуга детейлінг-студії (Корінь агрегата).
/// Містить інкапсульовану бізнес-логіку, оптимістичне блокування (RowVersion),
/// аудит і м'яке видалення (IsDeleted).
/// </summary>
public class Service
{
    private readonly List<ServiceImage> _images = new();
    private readonly List<PackageService> _packageServices = new();

    public long Id { get; private set; }
    public long CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int DurationMin { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = "system";
    public DateTime? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public ServiceCategory? Category { get; private set; }
    public ServiceSpec? Spec { get; private set; }
    public IReadOnlyCollection<ServiceImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<PackageService> PackageServices => _packageServices.AsReadOnly();

    // Приватний конструктор для EF Core
    private Service()
    {
    }

    public Service(
        long categoryId,
        string name,
        decimal price,
        int durationMin,
        string createdBy = "system",
        long id = 0)
    {
        SetCategory(categoryId);
        SetName(name);
        SetPrice(price);
        SetDuration(durationMin);

        CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "system" : createdBy.Trim();
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;

        if (id > 0)
        {
            Id = id;
        }
    }

    public void UpdateDetails(string name, long categoryId, int durationMin, string? updatedBy = null)
    {
        SetName(name);
        SetCategory(categoryId);
        SetDuration(durationMin);

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "system" : updatedBy.Trim();
    }

    public void UpdatePrice(decimal newPrice, string? updatedBy = null)
    {
        if (newPrice == Price)
        {
            throw new DomainException($"Нова ціна ({newPrice:F2}) збігається з поточною ціною послуги.");
        }

        SetPrice(newPrice);
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "system" : updatedBy.Trim();
    }

    public void AttachSpec(string materials, int warrantyMonths, string description)
    {
        if (Spec == null)
        {
            Spec = new ServiceSpec(Id, materials, warrantyMonths, description);
        }
        else
        {
            Spec.Update(materials, warrantyMonths, description);
        }
    }

    public ServiceImage AddImage(string url, bool isMain = false)
    {
        if (isMain)
        {
            foreach (var img in _images)
            {
                img.UnsetAsMain();
            }
        }
        else if (_images.Count == 0)
        {
            // Перше фото автоматично стає головним, якщо явного головного ще немає
            isMain = true;
        }

        var image = new ServiceImage(Id, url, isMain);
        _images.Add(image);
        return image;
    }

    public void SetMainImage(long imageId)
    {
        var target = _images.FirstOrDefault(i => i.Id == imageId);
        if (target == null)
        {
            throw new DomainException($"Зображення #{imageId} не належить даній послузі.");
        }

        foreach (var img in _images)
        {
            img.UnsetAsMain();
        }

        target.SetAsMain();
    }

    public void RemoveImage(long imageId)
    {
        var target = _images.FirstOrDefault(i => i.Id == imageId);
        if (target == null)
        {
            throw new DomainException($"Зображення #{imageId} не знайдено.");
        }

        var wasMain = target.IsMain;
        _images.Remove(target);

        // Якщо видалено головне фото, призначаємо перше з тих, що залишились
        if (wasMain && _images.Count > 0)
        {
            _images[0].SetAsMain();
        }
    }

    public void SoftDelete(string? deletedBy = null)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = string.IsNullOrWhiteSpace(deletedBy) ? "system" : deletedBy.Trim();
    }

    public void Restore(string? restoredBy = null)
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = string.IsNullOrWhiteSpace(restoredBy) ? "system" : restoredBy.Trim();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Назва послуги не може бути порожньою.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 200)
        {
            throw new DomainException("Назва послуги не може перевищувати 200 символів.");
        }

        Name = trimmed;
    }

    private void SetCategory(long categoryId)
    {
        if (categoryId <= 0)
        {
            throw new DomainException("Ідентифікатор категорії (CategoryId) повинен бути додатним.");
        }

        CategoryId = categoryId;
    }

    private void SetPrice(decimal price)
    {
        if (price <= 0)
        {
            throw new DomainException("Вартість послуги повинна бути більшою за нуль.");
        }

        Price = price;
    }

    private void SetDuration(int durationMin)
    {
        if (durationMin <= 0)
        {
            throw new DomainException("Тривалість послуги повинна бути більшою за 0 хвилин.");
        }

        DurationMin = durationMin;
    }
}
