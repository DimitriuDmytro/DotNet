namespace Reviews.Application.Common.Interfaces;

using MongoDB.Bson;

/// <summary>
/// Інтерфейс для оптимізованих запитів читання безпосередньо в DTO (Критерій 6).
/// Реалізація в інфраструктурному шарі виконує проекції на рівні MongoDB.
/// </summary>
public interface IReviewQueries
{
    Task<ReviewDetailDto?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default);
    Task<PagedResult<ReviewListDto>> GetPagedAsync(long? serviceId, int page, int pageSize, CancellationToken cancellationToken = default);
}
