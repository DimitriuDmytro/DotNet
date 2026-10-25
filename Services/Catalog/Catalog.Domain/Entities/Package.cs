namespace Catalog.Domain.Entities;

using Catalog.Domain.Exceptions;

/// <summary>
/// Комплексний пакет послуг детейлінгу (Корінь агрегата).
/// Зв'язаний з послугами відношенням M:N через сутність PackageService.
/// </summary>
public class Package
{
    private readonly List<PackageService> _packageServices = new();

    public long Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = "system";
    public DateTime? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public IReadOnlyCollection<PackageService> PackageServices => _packageServices.AsReadOnly();

    // Приватний конструктор для EF Core
    private Package()
    {
    }

    public Package(
        string name,
        decimal price,
        string? description = null,
        string createdBy = "system",
        long id = 0)
    {
        SetName(name);
        SetPrice(price);
        Description = description?.Trim();
        CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "system" : createdBy.Trim();
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;

        if (id > 0)
        {
            Id = id;
        }
    }

    public void UpdateDetails(string name, decimal price, string? description = null, string? updatedBy = null)
    {
        SetName(name);
        SetPrice(price);
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "system" : updatedBy.Trim();
    }

    public void UpdatePrice(decimal newPrice, string? updatedBy = null)
    {
        SetPrice(newPrice);
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "system" : updatedBy.Trim();
    }

    public PackageService AddService(long serviceId, int sortOrder)
    {
        if (serviceId <= 0)
        {
            throw new DomainException("Ідентифікатор послуги повинен бути додатним.");
        }

        if (_packageServices.Any(ps => ps.ServiceId == serviceId))
        {
            throw new DomainException($"Послуга #{serviceId} вже додана до цього пакета.");
        }

        var item = new PackageService(Id, serviceId, sortOrder, DateTime.UtcNow);
        _packageServices.Add(item);
        return item;
    }

    public void RemoveService(long serviceId)
    {
        var existing = _packageServices.FirstOrDefault(ps => ps.ServiceId == serviceId);
        if (existing == null)
        {
            throw new DomainException($"Послугу #{serviceId} не знайдено у складі даного пакета.");
        }

        _packageServices.Remove(existing);
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
            throw new DomainException("Назва пакета обов'язкова і не може бути порожньою.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 150)
        {
            throw new DomainException("Назва пакета не може перевищувати 150 символів.");
        }

        Name = trimmed;
    }

    private void SetPrice(decimal price)
    {
        if (price <= 0)
        {
            throw new DomainException("Вартість пакета повинна бути більшою за нуль.");
        }

        Price = price;
    }
}
