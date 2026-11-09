namespace Reviews.Application.Reviews.Queries.GetReviewsList;

/// <summary>
/// Запит отримання пагінованого списку відгуків з можливістю фільтрації за послугою з кешуванням.
/// </summary>
public record GetReviewsListQuery(
    long? ServiceId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ReviewListDto>>, ICacheableQuery
{
    public string CacheKey => ServiceId.HasValue
        ? ReviewsCacheKeys.ServiceReviews(ServiceId.Value, Page, PageSize)
        : ReviewsCacheKeys.ReviewsList(Page, PageSize);

    public TimeSpan L1Ttl => ReviewsCacheTtl.ReviewsList.L1Ttl;
    public TimeSpan L2Ttl => ReviewsCacheTtl.ReviewsList.L2Ttl;
    public TimeSpan? L1Sliding => ReviewsCacheTtl.ReviewsList.L1Sliding;
}
