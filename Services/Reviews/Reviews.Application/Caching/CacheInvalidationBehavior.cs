namespace Reviews.Application.Caching;

using MediatR;
using Microsoft.Extensions.Logging;

public sealed class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IReviewsTwoLevelCache _cache;
    private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger;

    public CacheInvalidationBehavior(
        IReviewsTwoLevelCache cache,
        ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is ICacheInvalidatingCommand cmd)
        {
            await _cache.RemoveManyAsync(cmd.InvalidationKeys, cmd.InvalidationReason, cancellationToken);
        }

        return response;
    }
}
