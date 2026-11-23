namespace Catalog.Api.Metrics;

using System.Diagnostics.Metrics;

/// <summary>
/// Бізнес-метрики та гістограми операцій мікросервісу Catalog (Критерії 6, 7).
/// </summary>
public sealed class CatalogMetrics
{
    public const string MeterName = "Catalog.Api";

    private readonly Meter _meter;
    private readonly Counter<long> _servicesCreated;
    private readonly Counter<long> _servicesUpdated;
    private readonly Counter<long> _priceChanged;
    private readonly Histogram<double> _operationDuration;
    private int _totalServicesCount;

    public CatalogMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _servicesCreated = _meter.CreateCounter<long>(
            "catalog.services.created",
            unit: "{service}",
            description: "Загальна кількість створених послуг у каталозі");

        _servicesUpdated = _meter.CreateCounter<long>(
            "catalog.services.updated",
            unit: "{service}",
            description: "Загальна кількість оновлень послуг");

        _priceChanged = _meter.CreateCounter<long>(
            "catalog.services.price_changed",
            unit: "{change}",
            description: "Загальна кількість змін вартості послуг");

        _operationDuration = _meter.CreateHistogram<double>(
            "catalog.operation.duration",
            unit: "ms",
            description: "Тривалість операцій каталогу послуг у мілісекундах");

        _meter.CreateObservableGauge(
            "catalog.services.total",
            () => _totalServicesCount,
            unit: "{service}",
            description: "Загальна кількість активних послуг у каталозі");
    }

    public void RecordServiceCreated()
    {
        _servicesCreated.Add(1);
        Interlocked.Increment(ref _totalServicesCount);
    }

    public void RecordServiceUpdated()
    {
        _servicesUpdated.Add(1);
    }

    public void RecordPriceChanged()
    {
        _priceChanged.Add(1);
    }

    public void RecordOperationDuration(double elapsedMilliseconds, string operation, string status = "success")
    {
        _operationDuration.Record(
            elapsedMilliseconds,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", status));
    }

    public void SetTotalServicesCount(int count)
    {
        Interlocked.Exchange(ref _totalServicesCount, Math.Max(0, count));
    }
}
