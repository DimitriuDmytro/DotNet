namespace Reviews.Domain.Exceptions;

/// <summary>
/// Доменний виняток для сигналізації порушення бізнес-правил та інваріантів (Критерій 2).
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
