namespace Platform.EventBus.Publisher;

using Platform.EventBus.Events;

/// <summary>
/// Інтерфейс для персистентної публікації інтеграційних подій (Критерій 5).
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(
        T @event,
        string exchange,
        string routingKey,
        CancellationToken ct = default) where T : IntegrationEvent;

    Task PublishAsync<T>(
        string exchange,
        string routingKey,
        T @event,
        CancellationToken ct = default) where T : IntegrationEvent;
}
