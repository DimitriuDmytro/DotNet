namespace Reviews.Application.Caching;

public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan L1Ttl { get; }
    TimeSpan L2Ttl { get; }
    TimeSpan? L1Sliding { get; }
}

public interface ICacheInvalidatingCommand
{
    IEnumerable<string> InvalidationKeys { get; }
    string InvalidationReason { get; }
}

public sealed record CacheEntrySettings(
    TimeSpan L1Ttl,
    TimeSpan L2Ttl,
    TimeSpan? L1Sliding = null,
    int Size = 1);
