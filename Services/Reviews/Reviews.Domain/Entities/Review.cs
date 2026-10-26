namespace Reviews.Domain.Entities;

using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

/// <summary>
/// Корінь агрегата «Відгук клієнта» (Критерій 2).
/// Інкапсулює бізнес-правила оцінювання, коментування та медіа-вкладень.
/// Не містить BSON-атрибутів (чистий домен).
/// </summary>
public class Review : BaseEntity
{
    private readonly List<ReviewComment> _comments = new();
    private readonly List<string> _photosBefore = new();
    private readonly List<string> _photosAfter = new();
    private readonly List<string> _pros = new();
    private readonly List<string> _cons = new();

    public long ServiceId { get; private set; }
    public string ServiceName { get; private set; } = string.Empty;
    public long? AppointmentId { get; private set; }
    public ObjectId? DiscussionId { get; private set; }
    public int Rating { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public bool VerifiedVisit { get; private set; }
    public Author Author { get; private set; } = null!;
    public string? VideoUrl { get; private set; }

    public IReadOnlyCollection<ReviewComment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<string> PhotosBefore => _photosBefore.AsReadOnly();
    public IReadOnlyCollection<string> PhotosAfter => _photosAfter.AsReadOnly();
    public IReadOnlyCollection<string> Pros => _pros.AsReadOnly();
    public IReadOnlyCollection<string> Cons => _cons.AsReadOnly();

    // Приватний конструктор без параметрів для BSON-десеріалізації
    private Review()
    {
    }

    public Review(
        long serviceId,
        string serviceName,
        int rating,
        string title,
        string text,
        Author author,
        long? appointmentId = null,
        bool verifiedVisit = false,
        ObjectId? discussionId = null,
        ObjectId? id = null)
    {
        if (id.HasValue && id.Value != ObjectId.Empty)
        {
            Id = id.Value;
        }

        SetService(serviceId, serviceName);
        SetRating(rating);
        SetContent(title, text);

        Author = author ?? throw new DomainException("Автор відгуку є обов'язковим.");
        AppointmentId = appointmentId > 0 ? appointmentId : null;
        VerifiedVisit = verifiedVisit;
        DiscussionId = discussionId;
    }

    public void UpdateDetails(int rating, string title, string text, string? videoUrl = null)
    {
        SetRating(rating);
        SetContent(title, text);
        if (videoUrl != null)
        {
            SetVideoUrl(videoUrl);
        }
        Touch();
    }

    public void AddComment(ReviewComment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);

        if (_comments.Count >= 3)
        {
            throw new DomainException("До відгуку дозволено додавати щонайбільше 3 відповіді/коментарі.");
        }

        _comments.Add(comment);
        Touch();
    }

    public void AddPhotos(IEnumerable<string>? before, IEnumerable<string>? after)
    {
        var changed = false;

        if (before != null)
        {
            foreach (var url in before.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                _photosBefore.Add(url.Trim());
                changed = true;
            }
        }

        if (after != null)
        {
            foreach (var url in after.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                _photosAfter.Add(url.Trim());
                changed = true;
            }
        }

        if (changed)
        {
            Touch();
        }
    }

    public void SetVideoUrl(string? videoUrl)
    {
        VideoUrl = string.IsNullOrWhiteSpace(videoUrl) ? null : videoUrl.Trim();
        Touch();
    }

    public void AddProsAndCons(IEnumerable<string>? pros, IEnumerable<string>? cons)
    {
        var changed = false;

        if (pros != null)
        {
            foreach (var item in pros.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                _pros.Add(item.Trim());
                changed = true;
            }
        }

        if (cons != null)
        {
            foreach (var item in cons.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                _cons.Add(item.Trim());
                changed = true;
            }
        }

        if (changed)
        {
            Touch();
        }
    }

    public void SetDiscussionId(ObjectId discussionId)
    {
        if (discussionId == ObjectId.Empty)
        {
            throw new DomainException("DiscussionId не може бути порожнім ObjectId.");
        }

        DiscussionId = discussionId;
        Touch();
    }

    private void SetService(long serviceId, string serviceName)
    {
        if (serviceId <= 0)
        {
            throw new DomainException("Ідентифікатор послуги (ServiceId) повинен бути додатним числом.");
        }

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new DomainException("Назва послуги є обов'язковою.");
        }

        ServiceId = serviceId;
        ServiceName = serviceName.Trim();
    }

    private void SetRating(int rating)
    {
        if (rating < 1 || rating > 5)
        {
            throw new DomainException($"Оцінка повинна бути цілим числом від 1 до 5. Отримано: {rating}.");
        }

        Rating = rating;
    }

    private void SetContent(string title, string text)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Заголовок відгуку не може бути порожнім.");
        }

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length < 3 || trimmedTitle.Length > 200)
        {
            throw new DomainException("Заголовок відгуку повинен містити від 3 до 200 символів.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainException("Текст відгуку не може бути порожнім.");
        }

        var trimmedText = text.Trim();
        if (trimmedText.Length < 5 || trimmedText.Length > 2000)
        {
            throw new DomainException("Текст відгуку повинен містити від 5 до 2000 символів.");
        }

        Title = trimmedTitle;
        Text = trimmedText;
    }
}
