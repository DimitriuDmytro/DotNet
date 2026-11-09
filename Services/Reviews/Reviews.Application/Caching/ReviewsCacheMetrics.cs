namespace Reviews.Application.Caching;

using System.Diagnostics.Metrics;

/// <summary>
/// Метрики кешування відгуків (Частина 6.3): hits, misses, invalidations, duration.
/// Тег cache.level зі значеннями "L1" (IMemoryCache) та "L2" (Redis).
/// </summary>
public sealed class ReviewsCacheMetrics
{
    public const string MeterName = "Reviews.Cache";
    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;
    private readonly Counter<long> _invalidations;
    private readonly Histogram<double> _duration;

    public ReviewsCacheMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _hits = meter.CreateCounter<long>("cache.hits", description: "Кількість влучень у кеш відгуків");
        _misses = meter.CreateCounter<long>("cache.misses", description: "Кількість промахів кешу відгуків");
        _invalidations = meter.CreateCounter<long>("cache.invalidations", description: "Кількість інвалідацій ключів відгуків");
        _duration = meter.CreateHistogram<double>("cache.operation.duration", unit: "ms", description: "Тривалість операцій з кешем відгуків");
    }

    public void Hit(string level) => _hits.Add(1, new KeyValuePair<string, object?>("cache.level", level));
    public void Miss(string level) => _misses.Add(1, new KeyValuePair<string, object?>("cache.level", level));
    public void Invalidation(string reason) => _invalidations.Add(1, new KeyValuePair<string, object?>("reason", reason));
    public void RecordDuration(double ms, string operation) => _duration.Record(ms, new KeyValuePair<string, object?>("operation", operation));
}
