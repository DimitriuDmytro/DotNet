namespace Reviews.Domain.Exceptions;

/// <summary>
/// Виняток бізнес-конфлікту (порушення унікальності або конфлікт версій при конкурентному доступі).
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
