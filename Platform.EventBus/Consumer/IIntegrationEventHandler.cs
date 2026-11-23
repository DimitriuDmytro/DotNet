namespace Platform.EventBus.Consumer;

using Platform.EventBus.Events;

/// <summary>
/// Інтерфейс обробника конкретного типу інтеграційної події (Критерій 7, 9).
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
