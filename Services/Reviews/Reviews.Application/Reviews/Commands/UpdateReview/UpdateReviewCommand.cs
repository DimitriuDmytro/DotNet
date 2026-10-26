namespace Reviews.Application.Reviews.Commands.UpdateReview;

/// <summary>
/// Команда оновлення відгуку з підтримкою оптимістичного блокування (Критерій 5).
/// </summary>
public record UpdateReviewCommand(
    string Id,
    int ExpectedVersion,
    int Rating,
    string Title,
    string Text,
    string? VideoUrl = null) : ICommand;
