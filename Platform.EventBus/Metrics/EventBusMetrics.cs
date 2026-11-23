namespace Platform.EventBus.Metrics;

using System.Diagnostics.Metrics;

/// <summary>
/// Метрики брокера повідомлень RabbitMQ (Критерій 13 бонус).
/// </summary>
public static class EventBusMetrics
{
    public const string MeterName = "Platform.Messaging";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    private static readonly Counter<long> PublishedEventsCounter =
        Meter.CreateCounter<long>("messaging.events.published", "events", "Number of published integration events");

    private static readonly Counter<long> ConsumedEventsCounter =
        Meter.CreateCounter<long>("messaging.events.consumed", "events", "Number of consumed integration events");

    private static readonly Counter<long> FailedEventsCounter =
        Meter.CreateCounter<long>("messaging.events.failed", "events", "Number of failed integration events");

    public static void RecordPublished(string eventType, string exchange)
    {
        PublishedEventsCounter.Add(1,
            new KeyValuePair<string, object?>("event.type", eventType),
            new KeyValuePair<string, object?>("messaging.destination", exchange));
    }

    public static void RecordConsumed(string eventType, string queue, string status)
    {
        ConsumedEventsCounter.Add(1,
            new KeyValuePair<string, object?>("event.type", eventType),
            new KeyValuePair<string, object?>("messaging.queue", queue),
            new KeyValuePair<string, object?>("status", status));
    }

    public static void RecordFailed(string eventType, string queue, string failureReason)
    {
        FailedEventsCounter.Add(1,
            new KeyValuePair<string, object?>("event.type", eventType),
            new KeyValuePair<string, object?>("messaging.queue", queue),
            new KeyValuePair<string, object?>("failure.reason", failureReason));
    }
}
