namespace Reviews.Application.Reviews.Commands.UpdateReview;

/// <summary>
/// Команда оновлення відгуку з підтримкою оптимістичного блокування та інвалідації кешу.
/// </summary>
public record UpdateReviewCommand(
    string Id,
    int ExpectedVersion,
    int Rating,
    string Title,
    string Text,
    string? VideoUrl = null) : ICommand, ICacheInvalidatingCommand
{
    public IEnumerable<string> InvalidationKeys => new[]
    {
        ReviewsCacheKeys.Review(Id),
        ReviewsCacheKeys.ReviewsList(1, 20)
    };

    public string InvalidationReason => $"Review updated {Id}";
}
