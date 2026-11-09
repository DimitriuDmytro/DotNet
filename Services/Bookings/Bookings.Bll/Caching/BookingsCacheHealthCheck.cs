namespace Bookings.Bll.Caching;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Health Check для дворівневого кешу Bookings:
/// Healthy - якщо і Redis (L2), і MemoryCache (L1) доступні;
/// Degraded - якщо Redis недоступний, але L1 живий;
/// Unhealthy - якщо L1 зламано.
/// </summary>
public sealed class BookingsCacheHealthCheck : IHealthCheck
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;

    public BookingsCacheHealthCheck(IMemoryCache memoryCache, IDistributedCache distributedCache)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        bool l1Healthy = true;
        try
        {
            var stats = (_memoryCache as MemoryCache)?.GetCurrentStatistics();
            if (stats is not null)
            {
                data["L1.CurrentEntryCount"] = stats.CurrentEntryCount;
                data["L1.CurrentEstimatedSize"] = stats.CurrentEstimatedSize ?? 0;
                data["L1.TotalHits"] = stats.TotalHits;
                data["L1.TotalMisses"] = stats.TotalMisses;
            }
            data["L1.Status"] = "Healthy";
        }
        catch (Exception ex)
        {
            l1Healthy = false;
            data["L1.Status"] = "Failed";
            data["L1.Error"] = ex.Message;
        }

        bool l2Healthy = true;
        try
        {
            const string testKey = "__bookings_cache_healthcheck_ping__";
            await _distributedCache.GetAsync(testKey, cancellationToken);
            data["L2.Status"] = "Healthy";
        }
        catch (Exception ex)
        {
            l2Healthy = false;
            data["L2.Status"] = "Unavailable";
            data["L2.Error"] = ex.Message;
        }

        if (l1Healthy && l2Healthy)
        {
            return HealthCheckResult.Healthy("Bookings L1 (Memory) and L2 (Redis) cache operational.", data);
        }

        if (l1Healthy && !l2Healthy)
        {
            return HealthCheckResult.Degraded("Bookings L2 (Redis) cache unavailable, operating on L1.", null, data);
        }

        return HealthCheckResult.Unhealthy("Bookings cache failed.", null, data);
    }
}
