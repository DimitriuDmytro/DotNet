namespace Catalog.Domain.Exceptions;

/// <summary>
/// Виняток при конфлікті стану або унікальності (наприклад, дублікат назви послуги чи пакета).
/// Транслюється у HTTP 409 Conflict.
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message)
    {
    }
}
