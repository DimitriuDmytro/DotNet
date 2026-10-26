namespace Reviews.Application.Reviews.Commands.DeleteReview;

/// <summary>
/// Команда видалення відгуку (Критерій 5).
/// </summary>
public record DeleteReviewCommand(string Id) : ICommand;
