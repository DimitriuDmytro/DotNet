namespace Reviews.Application.Reviews.Commands.UpdateReview;

using MediatR;
using MongoDB.Bson;

/// <summary>
/// Обробник команди оновлення відгуку (Критерій 5, 11).
/// </summary>
public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand>
{
    private readonly IReviewRepository _reviewRepository;

    public UpdateReviewCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var objectId = ObjectId.Parse(request.Id);
        var review = await _reviewRepository.GetByIdAsync(objectId, cancellationToken)
            ?? throw new NotFoundException($"Відгук з ID '{request.Id}' не знайдено.");

        review.UpdateDetails(request.Rating, request.Title, request.Text, request.VideoUrl);

        await _reviewRepository.UpdateAsync(review, request.ExpectedVersion, cancellationToken);
    }
}
