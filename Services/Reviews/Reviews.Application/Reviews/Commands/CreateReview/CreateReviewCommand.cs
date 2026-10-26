namespace Reviews.Application.Reviews.Commands.CreateReview;

/// <summary>
/// Команда створення нового відгуку (Критерій 5).
/// Повертає рядковий ідентифікатор створеного документа ObjectId.
/// </summary>
public record CreateReviewCommand(
    long ServiceId,
    string ServiceName,
    long? AppointmentId,
    string? DiscussionId,
    int Rating,
    string Title,
    string Text,
    bool VerifiedVisit,
    long AuthorClientId,
    string AuthorFullName,
    string? AuthorCarInfo,
    IReadOnlyList<string>? PhotosBefore = null,
    IReadOnlyList<string>? PhotosAfter = null,
    string? VideoUrl = null,
    IReadOnlyList<string>? Pros = null,
    IReadOnlyList<string>? Cons = null) : ICommand<string>;
