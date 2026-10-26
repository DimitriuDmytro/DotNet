namespace Reviews.Infrastructure.Persistence;

using AutoMapper;
using MongoDB.Bson;
using MongoDB.Driver;

/// <summary>
/// Реалізація оптимізованих запитів читання для CQRS (Критерій 6).
/// Виконує фільтрацію, пагінацію (Skip, Limit) та проекцію в DTO.
/// </summary>
public class ReviewQueries : IReviewQueries
{
    private readonly MongoDbContext _context;
    private readonly IMapper _mapper;

    public ReviewQueries(MongoDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ReviewDetailDto?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var review = await _context.Reviews
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        return review == null ? null : _mapper.Map<ReviewDetailDto>(review);
    }

    public async Task<PagedResult<ReviewListDto>> GetPagedAsync(
        long? serviceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filter = serviceId.HasValue
            ? Builders<Review>.Filter.Eq(r => r.ServiceId, serviceId.Value)
            : Builders<Review>.Filter.Empty;

        var countTask = _context.Reviews.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var itemsTask = _context.Reviews
            .Find(filter)
            .SortByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(countTask, itemsTask);

        var totalCount = (int)await countTask;
        var entities = await itemsTask;
        var dtos = _mapper.Map<IReadOnlyList<ReviewListDto>>(entities);

        return new PagedResult<ReviewListDto>(dtos, totalCount, page, pageSize);
    }
}
