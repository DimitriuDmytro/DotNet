namespace Catalog.Domain.Exceptions;

/// <summary>
/// Базовий доменний виняток для бізнес-правил мікросервісу Catalog.
/// За замовчуванням транслюється у HTTP 409 Conflict або обробляється за типом.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
