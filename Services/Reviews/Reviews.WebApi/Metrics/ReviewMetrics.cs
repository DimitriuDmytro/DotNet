namespace Reviews.WebApi.Metrics;

using System.Diagnostics.Metrics;

/// <summary>
/// Бізнес-метрики та гістограми операцій мікросервісу Reviews (Критерії 6, 7).
/// </summary>
public sealed class ReviewMetrics
{
    public const string MeterName = "Reviews.WebApi";

    private readonly Meter _meter;
    private readonly Counter<long> _reviewsCreated;
    private readonly Counter<long> _reviewsDeleted;
    private readonly Histogram<double> _operationDuration;
    private int _totalReviewsCount;

    public ReviewMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _reviewsCreated = _meter.CreateCounter<long>(
            "reviews.created",
            unit: "{review}",
            description: "Загальна кількість створених відгуків");

        _reviewsDeleted = _meter.CreateCounter<long>(
            "reviews.deleted",
            unit: "{review}",
            description: "Загальна кількість видалених відгуків");

        _operationDuration = _meter.CreateHistogram<double>(
            "reviews.operation.duration",
            unit: "ms",
            description: "Тривалість операцій з відгуками у мілісекундах");

        _meter.CreateObservableGauge(
            "reviews.total",
            () => _totalReviewsCount,
            unit: "{review}",
            description: "Загальна кількість відгуків у системі");
    }

    public void RecordReviewCreated()
    {
        _reviewsCreated.Add(1);
        Interlocked.Increment(ref _totalReviewsCount);
    }

    public void RecordReviewDeleted()
    {
        _reviewsDeleted.Add(1);
        if (_totalReviewsCount > 0)
        {
            Interlocked.Decrement(ref _totalReviewsCount);
        }
    }

    public void RecordOperationDuration(double elapsedMilliseconds, string operation, string status = "success")
    {
        _operationDuration.Record(
            elapsedMilliseconds,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", status));
    }

    public void SetTotalReviewsCount(int count)
    {
        Interlocked.Exchange(ref _totalReviewsCount, Math.Max(0, count));
    }
}
