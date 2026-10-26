namespace Reviews.Application.Reviews.Queries.GetReviewsList;

using MediatR;

/// <summary>
/// Обробник запиту пагінованого списку відгуків (Критерій 6).
/// Виконує валідацію/нормалізацію меж пагінації Math.Clamp(PageSize, 1, 100).
/// </summary>
public class GetReviewsListQueryHandler : IRequestHandler<GetReviewsListQuery, PagedResult<ReviewListDto>>
{
    private readonly IReviewQueries _reviewQueries;

    public GetReviewsListQueryHandler(IReviewQueries reviewQueries)
    {
        _reviewQueries = reviewQueries;
    }

    public async Task<PagedResult<ReviewListDto>> Handle(GetReviewsListQuery request, CancellationToken cancellationToken)
    {
        var clampedPageSize = Math.Clamp(request.PageSize, 1, 100);
        var clampedPage = Math.Max(1, request.Page);

        return await _reviewQueries.GetPagedAsync(request.ServiceId, clampedPage, clampedPageSize, cancellationToken);
    }
}
