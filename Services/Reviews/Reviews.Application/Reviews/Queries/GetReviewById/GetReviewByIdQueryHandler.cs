namespace Reviews.Application.Reviews.Queries.GetReviewById;

using MediatR;
using MongoDB.Bson;

/// <summary>
/// Обробник запиту отримання відгуку за ID (Критерій 6).
/// Викликає IReviewQueries для прямої проекції в DTO.
/// </summary>
public class GetReviewByIdQueryHandler : IRequestHandler<GetReviewByIdQuery, ReviewDetailDto?>
{
    private readonly IReviewQueries _reviewQueries;

    public GetReviewByIdQueryHandler(IReviewQueries reviewQueries)
    {
        _reviewQueries = reviewQueries;
    }

    public async Task<ReviewDetailDto?> Handle(GetReviewByIdQuery request, CancellationToken cancellationToken)
    {
        var objectId = ObjectId.Parse(request.Id);
        return await _reviewQueries.GetByIdAsync(objectId, cancellationToken);
    }
}
