namespace Reviews.Application.Reviews.Queries.GetReviewById;

/// <summary>
/// Запит отримання детальної інформації про відгук за ID з підтримкою кешування.
/// </summary>
public record GetReviewByIdQuery(string Id) : IQuery<ReviewDetailDto?>, ICacheableQuery
{
    public string CacheKey => ReviewsCacheKeys.Review(Id);
    public TimeSpan L1Ttl => ReviewsCacheTtl.Review.L1Ttl;
    public TimeSpan L2Ttl => ReviewsCacheTtl.Review.L2Ttl;
    public TimeSpan? L1Sliding => ReviewsCacheTtl.Review.L1Sliding;
}
