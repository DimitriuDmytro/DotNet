namespace Reviews.Application.Reviews.Commands.DeleteReview;

using MediatR;
using MongoDB.Bson;

/// <summary>
/// Обробник команди видалення відгуку (Критерій 5).
/// </summary>
public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand>
{
    private readonly IReviewRepository _reviewRepository;

    public DeleteReviewCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var objectId = ObjectId.Parse(request.Id);
        var review = await _reviewRepository.GetByIdAsync(objectId, cancellationToken)
            ?? throw new NotFoundException($"Відгук з ID '{request.Id}' не знайдено.");

        await _reviewRepository.DeleteAsync(objectId, cancellationToken);
    }
}
