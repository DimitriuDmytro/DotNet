namespace Platform.Aggregator.Services;

using System.Diagnostics.Metrics;

public sealed class AggregatorCacheMetrics
{
    public const string MeterName = "Aggregator.Cache";
    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;
    private readonly Histogram<double> _duration;

    public AggregatorCacheMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _hits = meter.CreateCounter<long>("cache.hits", description: "Кількість влучень в кеш агрегованих відповідей");
        _misses = meter.CreateCounter<long>("cache.misses", description: "Кількість промахів кешу агрегатора");
        _duration = meter.CreateHistogram<double>("cache.operation.duration", unit: "ms", description: "Тривалість операцій кешування агрегатора");
    }

    public void Hit() => _hits.Add(1, new KeyValuePair<string, object?>("cache.level", "L2"));
    public void Miss() => _misses.Add(1, new KeyValuePair<string, object?>("cache.level", "L2"));
    public void RecordDuration(double ms) => _duration.Record(ms);
}
