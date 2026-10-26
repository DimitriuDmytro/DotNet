namespace Reviews.Domain.ValueObjects;

using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

/// <summary>
/// Незмінний Value Object коментаря/відповіді на відгук (Критерій 3).
/// Зберігається як елемент масиву comments[] у MongoDB.
/// </summary>
public sealed class ReviewComment : ValueObject
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "master", "client"
    };

    public string AuthorName { get; }
    public string Role { get; }
    public string Text { get; }
    public DateTime CreatedAt { get; }

    public ReviewComment(string authorName, string role, string text, DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(authorName))
        {
            throw new DomainException("Ім'я автора коментаря є обов'язковим.");
        }

        if (string.IsNullOrWhiteSpace(role) || !AllowedRoles.Contains(role.Trim()))
        {
            throw new DomainException("Роль автора коментаря повинна бути однією з: 'admin', 'master', 'client'.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainException("Текст коментаря не може бути порожнім.");
        }

        AuthorName = authorName.Trim();
        Role = role.Trim().ToLowerInvariant();
        Text = text.Trim();
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AuthorName;
        yield return Role;
        yield return Text;
        yield return CreatedAt;
    }
}
