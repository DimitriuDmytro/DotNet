namespace Reviews.Application.Reviews.Queries.GetReviewById;

/// <summary>
/// Запит отримання детальної інформації про відгук за ID (Критерій 6).
/// </summary>
public record GetReviewByIdQuery(string Id) : IQuery<ReviewDetailDto?>;
