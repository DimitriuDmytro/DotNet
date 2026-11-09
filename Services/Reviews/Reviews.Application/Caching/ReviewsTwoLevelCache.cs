namespace Reviews.Application.Caching;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public sealed class ReviewsTwoLevelCache : IReviewsTwoLevelCache
{
    private static readonly ActivitySource Source = new("Reviews.Cache");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ReviewsCacheMetrics _metrics;
    private readonly ILogger<ReviewsTwoLevelCache> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public ReviewsTwoLevelCache(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ReviewsCacheMetrics metrics,
        ILogger<ReviewsTwoLevelCache> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntrySettings settings,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("Cache.GetOrCreate");
        activity?.SetTag("cache.key", key);

        var effectiveSettings = settings;
        if (settings.L1Ttl >= settings.L2Ttl)
        {
            var adjustedL1 = TimeSpan.FromTicks(settings.L2Ttl.Ticks / 2);
            _logger.LogWarning("Reviews.Cache: L1Ttl >= L2Ttl для ключа '{Key}'. Скориговано L1 до {Adjusted}", key, adjustedL1);
            effectiveSettings = settings with { L1Ttl = adjustedL1 };
        }

        // 1. Спроба читання з L1
        if (_memoryCache.TryGetValue(key, out T? l1Value))
        {
            sw.Stop();
            _metrics.Hit("L1");
            _metrics.RecordDuration(sw.Elapsed.TotalMilliseconds, "Get_L1");
            activity?.SetTag("cache.level", "L1");
            activity?.SetTag("cache.hit", true);
            _logger.LogDebug("Cache hit {CacheKey} at L1", key);
            return l1Value;
        }

        _metrics.Miss("L1");
        _logger.LogDebug("Cache miss {CacheKey} at L1", key);

        // Stampede protection
        var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(ct);
        try
        {
            if (_memoryCache.TryGetValue(key, out l1Value))
            {
                _metrics.Hit("L1");
                return l1Value;
            }

            // 2. Спроба читання з L2 (Redis)
            byte[]? l2Bytes = null;
            try
            {
                l2Bytes = await _distributedCache.GetAsync(key, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "L2 cache unavailable for {CacheKey}", key);
            }

            if (l2Bytes is not null && l2Bytes.Length > 0)
            {
                try
                {
                    var l2Value = JsonSerializer.Deserialize<T>(l2Bytes, JsonOptions);
                    if (l2Value is not null)
                    {
                        sw.Stop();
                        _metrics.Hit("L2");
                        _metrics.RecordDuration(sw.Elapsed.TotalMilliseconds, "Get_L2");
                        activity?.SetTag("cache.level", "L2");
                        activity?.SetTag("cache.hit", true);
                        _logger.LogDebug("Cache hit {CacheKey} at L2", key);

                        SetL1(key, l2Value, effectiveSettings);
                        return l2Value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Помилка десеріалізації з L2 для '{CacheKey}'", key);
                }
            }

            _metrics.Miss("L2");
            _logger.LogDebug("Cache miss {CacheKey} at L2", key);

            // 3. Звернення до обробника запиту (БД)
            var dbValue = await factory(ct);
            if (dbValue is null)
            {
                return default;
            }

            // 4. Запис у L2 та L1
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(dbValue, JsonOptions);
                var distributedOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = effectiveSettings.L2Ttl
                };
                await _distributedCache.SetAsync(key, bytes, distributedOptions, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "L2 cache unavailable for {CacheKey} при записі", key);
            }

            SetL1(key, dbValue, effectiveSettings);

            sw.Stop();
            _metrics.RecordDuration(sw.Elapsed.TotalMilliseconds, "GetOrCreate_Db");
            return dbValue;
        }
        finally
        {
            keyLock.Release();
            if (keyLock.CurrentCount == 1)
            {
                _locks.TryRemove(key, out _);
            }
        }
    }

    public async Task RemoveAsync(string key, string reason, CancellationToken ct)
    {
        using var activity = Source.StartActivity("Cache.Remove");
        activity?.SetTag("cache.key", key);
        activity?.SetTag("cache.reason", reason);

        _memoryCache.Remove(key);
        try
        {
            await _distributedCache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "L2 cache unavailable for {CacheKey} при видаленні", key);
        }

        _metrics.Invalidation(reason);
        _logger.LogInformation("Cache invalidated {CacheKeys}, reason {Reason}", key, reason);
    }

    public async Task RemoveManyAsync(IEnumerable<string> keys, string reason, CancellationToken ct)
    {
        var keyList = keys.ToList();
        using var activity = Source.StartActivity("Cache.RemoveMany");
        activity?.SetTag("cache.reason", reason);
        activity?.SetTag("cache.keys_count", keyList.Count);

        foreach (var key in keyList)
        {
            _memoryCache.Remove(key);
            try
            {
                await _distributedCache.RemoveAsync(key, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "L2 cache unavailable for {CacheKey} при пакетному видаленні", key);
            }
        }

        _metrics.Invalidation(reason);
        _logger.LogInformation("Cache invalidated {CacheKeys}, reason {Reason}", string.Join(", ", keyList), reason);
    }

    private void SetL1<T>(string key, T value, CacheEntrySettings settings)
    {
        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = settings.L1Ttl,
            SlidingExpiration = settings.L1Sliding,
            Size = settings.Size > 0 ? settings.Size : 1,
            Priority = CacheItemPriority.Normal
        };

        _memoryCache.Set(key, value, entryOptions);
    }
}
