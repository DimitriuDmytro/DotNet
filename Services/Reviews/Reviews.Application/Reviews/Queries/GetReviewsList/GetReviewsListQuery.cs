namespace Reviews.Application.Reviews.Queries.GetReviewsList;

/// <summary>
/// Запит отримання пагінованого списку відгуків з можливістю фільтрації за послугою (Критерій 6).
/// </summary>
public record GetReviewsListQuery(
    long? ServiceId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ReviewListDto>>;
