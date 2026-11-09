namespace Reviews.Application.Reviews.Commands.CreateReview;

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;

/// <summary>
/// Обробник команди створення відгуку (Критерій 5).
/// Працює виключно через IReviewRepository.
/// </summary>
public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, string>
{
    private static readonly ActivitySource Source = new("Reviews.Application");
    private readonly IReviewRepository _reviewRepository;
    private readonly ILogger<CreateReviewCommandHandler> _logger;

    public CreateReviewCommandHandler(IReviewRepository reviewRepository, ILogger<CreateReviewCommandHandler>? logger = null)
    {
        _reviewRepository = reviewRepository;
        _logger = logger ?? NullLogger<CreateReviewCommandHandler>.Instance;
    }

    public async Task<string> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        using var activity = Source.StartActivity("CreateReview");
        activity?.SetTag("service.id", request.ServiceId);
        activity?.SetTag("client.id", request.AuthorClientId);
        activity?.SetTag("review.rating", request.Rating);

        _logger.LogInformation(
            "Створення відгуку від клієнта #{ClientId} для послуги #{ServiceId} з оцінкою {Rating}",
            request.AuthorClientId,
            request.ServiceId,
            request.Rating);

        var author = new Author(request.AuthorClientId, request.AuthorFullName, request.AuthorCarInfo);

        ObjectId? discussionId = null;
        if (!string.IsNullOrWhiteSpace(request.DiscussionId) && ObjectId.TryParse(request.DiscussionId, out var parsedDiscId))
        {
            discussionId = parsedDiscId;
        }

        var review = new Review(
            serviceId: request.ServiceId,
            serviceName: request.ServiceName,
            author: author,
            rating: request.Rating,
            title: request.Title,
            text: request.Text,
            appointmentId: request.AppointmentId,
            discussionId: discussionId,
            verifiedVisit: request.VerifiedVisit);

        if (request.PhotosBefore != null || request.PhotosAfter != null)
        {
            review.AddPhotos(request.PhotosBefore, request.PhotosAfter);
        }

        if (!string.IsNullOrWhiteSpace(request.VideoUrl))
        {
            review.SetVideoUrl(request.VideoUrl);
        }

        if (request.Pros != null || request.Cons != null)
        {
            review.AddProsAndCons(request.Pros, request.Cons);
        }

        await _reviewRepository.AddAsync(review, cancellationToken);

        activity?.SetTag("review.id", review.Id.ToString());
        _logger.LogInformation("Відгук {ReviewId} успішно збережено у MongoDB", review.Id);

        return review.Id.ToString();
    }
}
