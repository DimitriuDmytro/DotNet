namespace Catalog.Domain.Exceptions;

/// <summary>
/// Виняток валідації вхідних даних або доменних інваріантів.
/// Транслюється у HTTP 400 Bad Request.
/// </summary>
public class ValidationException : DomainException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>
        {
            { "General", new[] { message } }
        };
    }

    public ValidationException(string propertyName, string errorMessage)
        : base(errorMessage)
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("Виникли одна або декілька помилок валідації.")
    {
        Errors = errors;
    }
}
