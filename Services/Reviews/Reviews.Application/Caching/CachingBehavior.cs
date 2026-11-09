namespace Reviews.Application.Caching;

using MediatR;
using Microsoft.Extensions.Logging;

public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IReviewsTwoLevelCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IReviewsTwoLevelCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheable)
        {
            return await next();
        }

        var settings = new CacheEntrySettings(
            cacheable.L1Ttl,
            cacheable.L2Ttl,
            cacheable.L1Sliding);

        var cachedResponse = await _cache.GetOrCreateAsync(
            cacheable.CacheKey,
            async ct => await next(),
            settings,
            cancellationToken);

        return cachedResponse!;
    }
}
