namespace Bookings.Bll.Caching;

public interface IBookingsTwoLevelCache
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntrySettings settings,
        CancellationToken ct);

    Task RemoveAsync(string key, string reason, CancellationToken ct);

    Task RemoveManyAsync(IEnumerable<string> keys, string reason, CancellationToken ct);
}
