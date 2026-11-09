namespace Reviews.Application.Reviews.Commands.DeleteReview;

/// <summary>
/// Команда видалення відгуку з інвалідацією кешу.
/// </summary>
public record DeleteReviewCommand(string Id) : ICommand, ICacheInvalidatingCommand
{
    public IEnumerable<string> InvalidationKeys => new[]
    {
        ReviewsCacheKeys.Review(Id),
        ReviewsCacheKeys.ReviewsList(1, 20)
    };

    public string InvalidationReason => $"Review deleted {Id}";
}
