namespace Reviews.Domain.Entities;

using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

/// <summary>
/// Сутність «Тема обговорення послуги/пакета» (Критерій 2).
/// Відповідає колекції discussions у MongoDB.
/// </summary>
public class Discussion : BaseEntity
{
    private List<string> _tags = new();

    public string TargetType { get; private set; } = "service";
    public long TargetId { get; private set; }
    public string TargetName { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public bool IsPinned { get; private set; }

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public Discussion()
    {
    }

    public Discussion(
        string targetType,
        long targetId,
        string targetName,
        string title,
        IEnumerable<string>? tags = null,
        bool isPinned = false,
        ObjectId? id = null)
    {
        if (id.HasValue && id.Value != ObjectId.Empty)
        {
            Id = id.Value;
        }

        SetTarget(targetType, targetId, targetName);
        SetTitle(title);
        IsPinned = isPinned;

        if (tags != null)
        {
            foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                _tags.Add(tag.Trim());
            }
        }
    }

    public void Pin()
    {
        IsPinned = true;
        Touch();
    }

    public void Unpin()
    {
        IsPinned = false;
        Touch();
    }

    public void UpdateTitle(string title)
    {
        SetTitle(title);
        Touch();
    }

    private void SetTarget(string targetType, long targetId, string targetName)
    {
        var normalizedType = targetType?.Trim().ToLowerInvariant();
        if (normalizedType != "service" && normalizedType != "package")
        {
            throw new DomainException("Тип об'єкта обговорення (TargetType) повинен бути 'service' або 'package'.");
        }

        if (targetId <= 0)
        {
            throw new DomainException("Ідентифікатор об'єкта (TargetId) повинен бути додатним числом.");
        }

        if (string.IsNullOrWhiteSpace(targetName))
        {
            throw new DomainException("Назва об'єкта обговорення (TargetName) є обов'язковою.");
        }

        TargetType = normalizedType;
        TargetId = targetId;
        TargetName = targetName.Trim();
    }

    private void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Заголовок теми не може бути порожнім.");
        }

        var trimmed = title.Trim();
        if (trimmed.Length < 5 || trimmed.Length > 200)
        {
            throw new DomainException("Заголовок теми повинен містити від 5 до 200 символів.");
        }

        Title = trimmed;
    }
}
