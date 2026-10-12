namespace Bookings.Domain.Exceptions;

/// <summary>
/// Виняток валідації вхідних даних бізнес-логіки.
/// Транслюється в HTTP 400 Bad Request.
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) 
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message, IDictionary<string, string[]> errors) 
        : base(message)
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : base($"Помилка валідації для поля '{field}': {error}")
    {
        Errors = new Dictionary<string, string[]>
        {
            [field] = new[] { error }
        };
    }
}
