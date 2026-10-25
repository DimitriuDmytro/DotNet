namespace Catalog.Domain.Exceptions;

/// <summary>
/// Виняток, що сигналізує про відсутність запитуваної сутності.
/// Транслюється у HTTP 404 Not Found.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object Key { get; }

    public EntityNotFoundException(string entityName, object key)
        : base($"Сутність '{entityName}' з ідентифікатором '{key}' не знайдена.")
    {
        EntityName = entityName;
        Key = key;
    }

    public EntityNotFoundException(string message) : base(message)
    {
        EntityName = string.Empty;
        Key = string.Empty;
    }
}
