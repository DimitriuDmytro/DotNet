namespace Catalog.Bll.Caching;

public sealed record CacheEntrySettings(
    TimeSpan L1Ttl,
    TimeSpan L2Ttl,
    TimeSpan? L1Sliding = null,
    int Size = 1);

public interface ITwoLevelCache
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntrySettings settings,
        CancellationToken ct);

    Task RemoveAsync(string key, string reason, CancellationToken ct);

    Task RemoveManyAsync(IEnumerable<string> keys, string reason, CancellationToken ct);
}
