namespace Bookings.Api.Metrics;

using System.Diagnostics.Metrics;

/// <summary>
/// Бізнес-метрики та гістограми операцій мікросервісу Bookings (Критерії 6, 7).
/// </summary>
public sealed class BookingMetrics
{
    public const string MeterName = "Bookings.Api";

    private readonly Meter _meter;
    private readonly Counter<long> _bookingsCreated;
    private readonly Counter<long> _bookingsStatusChanged;
    private readonly Counter<long> _bookingsCancelled;
    private readonly Histogram<double> _operationDuration;
    private int _activeBookingsCount;

    public BookingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _bookingsCreated = _meter.CreateCounter<long>(
            "bookings.created",
            unit: "{booking}",
            description: "Загальна кількість створених бронювань");

        _bookingsStatusChanged = _meter.CreateCounter<long>(
            "bookings.status_changed",
            unit: "{change}",
            description: "Загальна кількість змін статусів бронювань");

        _bookingsCancelled = _meter.CreateCounter<long>(
            "bookings.cancelled",
            unit: "{booking}",
            description: "Загальна кількість скасованих бронювань");

        _operationDuration = _meter.CreateHistogram<double>(
            "bookings.operation.duration",
            unit: "ms",
            description: "Тривалість бізнес-операцій бронювання у мілісекундах");

        _meter.CreateObservableGauge(
            "bookings.active_count",
            () => _activeBookingsCount,
            unit: "{booking}",
            description: "Поточна кількість активних/очікуючих бронювань");
    }

    public void RecordBookingCreated()
    {
        _bookingsCreated.Add(1);
        Interlocked.Increment(ref _activeBookingsCount);
    }

    public void RecordStatusChanged(string fromStatus, string toStatus)
    {
        _bookingsStatusChanged.Add(1, new KeyValuePair<string, object?>("from_status", fromStatus), new KeyValuePair<string, object?>("to_status", toStatus));

        if (toStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
            toStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
        {
            if (_activeBookingsCount > 0)
            {
                Interlocked.Decrement(ref _activeBookingsCount);
            }
        }
    }

    public void RecordBookingCancelled()
    {
        _bookingsCancelled.Add(1);
        if (_activeBookingsCount > 0)
        {
            Interlocked.Decrement(ref _activeBookingsCount);
        }
    }

    public void RecordOperationDuration(double elapsedMilliseconds, string operation, string status = "success")
    {
        _operationDuration.Record(
            elapsedMilliseconds,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", status));
    }

    public void SetActiveBookingsCount(int count)
    {
        Interlocked.Exchange(ref _activeBookingsCount, Math.Max(0, count));
    }
}
