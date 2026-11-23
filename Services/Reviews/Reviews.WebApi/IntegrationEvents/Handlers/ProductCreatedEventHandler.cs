namespace Reviews.WebApi.IntegrationEvents.Handlers;

using Microsoft.Extensions.Logging;
using Platform.EventBus.Consumer;
using Platform.EventBus.Events;
using Reviews.Domain.Interfaces;

/// <summary>
/// Обробник події створення послуги у контексті Reviews (ідемпотентна фіксація).
/// </summary>
public class ProductCreatedEventHandler : IIntegrationEventHandler<ProductCreatedEvent>
{
    private readonly IProcessedEventTracker _tracker;
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(
        IProcessedEventTracker tracker,
        ILogger<ProductCreatedEventHandler> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    public async Task HandleAsync(ProductCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        var isNew = await _tracker.TryTrackAsync(@event.EventId, cancellationToken);
        if (!isNew)
        {
            _logger.LogInformation("Duplicate event {EventId} skipped", @event.EventId);
            return;
        }

        _logger.LogInformation("Acknowledged product created event for ProductId {ProductId}: '{ProductName}'",
            @event.ProductId, @event.ProductName);
    }
}
