namespace Catalog.Domain.Entities;

using Catalog.Domain.Exceptions;

/// <summary>
/// Зображення галереї послуги (1:N із Service).
/// Має прапорець IsMain. У кожної послуги може бути щонайбільше одне головне фото.
/// </summary>
public class ServiceImage
{
    public long Id { get; private set; }
    public long ServiceId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public bool IsMain { get; private set; }

    public Service? Service { get; private set; }

    // Приватний конструктор для EF Core
    private ServiceImage()
    {
    }

    public ServiceImage(long serviceId, string url, bool isMain = false, long id = 0)
    {
        ServiceId = serviceId;
        UpdateUrl(url);
        IsMain = isMain;
        if (id > 0)
        {
            Id = id;
        }
    }

    public void UpdateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("URL-адреса зображення не може бути порожньою.");
        }

        var trimmed = url.Trim();
        if (trimmed.Length > 500)
        {
            throw new DomainException("URL-адреса зображення не може перевищувати 500 символів.");
        }

        Url = trimmed;
    }

    public void SetAsMain()
    {
        IsMain = true;
    }

    public void UnsetAsMain()
    {
        IsMain = false;
    }
}
