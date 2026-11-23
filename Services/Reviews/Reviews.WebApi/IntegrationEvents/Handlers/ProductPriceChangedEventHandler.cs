namespace Reviews.WebApi.IntegrationEvents.Handlers;

using Microsoft.Extensions.Logging;
using Platform.EventBus.Consumer;
using Platform.EventBus.Events;
using Reviews.Domain.Interfaces;

/// <summary>
/// Обробник події зміни ціни у контексті Reviews (ідемпотентна фіксація, без зміни відгуків).
/// </summary>
public class ProductPriceChangedEventHandler : IIntegrationEventHandler<ProductPriceChangedEvent>
{
    private readonly IProcessedEventTracker _tracker;
    private readonly ILogger<ProductPriceChangedEventHandler> _logger;

    public ProductPriceChangedEventHandler(
        IProcessedEventTracker tracker,
        ILogger<ProductPriceChangedEventHandler> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    public async Task HandleAsync(ProductPriceChangedEvent @event, CancellationToken cancellationToken = default)
    {
        var isNew = await _tracker.TryTrackAsync(@event.EventId, cancellationToken);
        if (!isNew)
        {
            _logger.LogInformation("Duplicate event {EventId} skipped", @event.EventId);
            return;
        }

        _logger.LogInformation(
            "Acknowledged price change event for ProductId {ProductId}: {OldPrice} -> {NewPrice}",
            @event.ProductId,
            @event.OldPrice,
            @event.NewPrice);
    }
}
