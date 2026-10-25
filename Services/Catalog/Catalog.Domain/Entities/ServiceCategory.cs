namespace Catalog.Domain.Entities;

using Catalog.Domain.Exceptions;

/// <summary>
/// Категорія послуг детейлінг-студії (наприклад, "Мийка", "Полірування", "Захист", "Салон").
/// </summary>
public class ServiceCategory
{
    private readonly List<Service> _services = new();

    public long Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public IReadOnlyCollection<Service> Services => _services.AsReadOnly();

    // Приватний конструктор для EF Core
    private ServiceCategory()
    {
    }

    public ServiceCategory(string name, string? description = null, long id = 0)
    {
        SetName(name);
        Description = description?.Trim();
        if (id > 0)
        {
            Id = id;
        }
    }

    public void Update(string name, string? description = null)
    {
        SetName(name);
        Description = description?.Trim();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Назва категорії не може бути порожньою.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 100)
        {
            throw new DomainException("Назва категорії не може перевищувати 100 символів.");
        }

        Name = trimmed;
    }
}
