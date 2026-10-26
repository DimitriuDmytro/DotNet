namespace Reviews.Application.Reviews.Commands.CreateReview;

using MediatR;
using MongoDB.Bson;

/// <summary>
/// Обробник команди створення відгуку (Критерій 5).
/// Працює виключно через IReviewRepository.
/// </summary>
public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, string>
{
    private readonly IReviewRepository _reviewRepository;

    public CreateReviewCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<string> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
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

        return review.Id.ToString();
    }
}
