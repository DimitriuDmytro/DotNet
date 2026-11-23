namespace Reviews.Application.Reviews.Commands.UpdateServiceNameInReviews;

using MediatR;

/// <summary>
/// MediatR-команда для каскадного оновлення денормалізованої назви послуги в колекціях reviews та workReports (Критерій 10).
/// </summary>
public record UpdateServiceNameInReviewsCommand(
    long ServiceId,
    string NewName,
    DateTime OccurredAt) : IRequest;
