namespace Reviews.Domain.Exceptions;

/// <summary>
/// Виняток відсутності шуканого документа або сутності.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"Документ '{entityName}' з ідентифікатором '{key}' не знайдено.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
