namespace Bookings.Domain.Exceptions;

/// <summary>
/// Доменний виняток, що сигналізує про відсутність запитуваної сутності (HTTP 404).
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"Сутність '{entityName}' з ідентифікатором '{key}' не знайдена або була видалена.")
    {
    }
}
