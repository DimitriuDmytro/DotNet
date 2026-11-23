namespace Reviews.WebApi.IntegrationEvents.Handlers;

using Microsoft.Extensions.Logging;
using Platform.EventBus.Consumer;
using Platform.EventBus.Events;
using Reviews.Application.Caching;
using Reviews.Domain.Interfaces;

/// <summary>
/// Обробник події видалення послуги у контексті Reviews (ідемпотентна фіксація та скидання кешу).
/// </summary>
public class ProductDeletedEventHandler : IIntegrationEventHandler<ProductDeletedEvent>
{
    private readonly IProcessedEventTracker _tracker;
    private readonly IReviewsTwoLevelCache? _cache;
    private readonly ILogger<ProductDeletedEventHandler> _logger;

    public ProductDeletedEventHandler(
        IProcessedEventTracker tracker,
        ILogger<ProductDeletedEventHandler> logger,
        IReviewsTwoLevelCache? cache = null)
    {
        _tracker = tracker;
        _logger = logger;
        _cache = cache;
    }

    public async Task HandleAsync(ProductDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        var isNew = await _tracker.TryTrackAsync(@event.EventId, cancellationToken);
        if (!isNew)
        {
            _logger.LogInformation("Duplicate event {EventId} skipped", @event.EventId);
            return;
        }

        _logger.LogInformation("Acknowledged product deleted event for ProductId {ProductId}", @event.ProductId);

        if (_cache != null)
        {
            await _cache.RemoveAsync($"reviews:service:{@event.ProductId}", "Product deleted via event", cancellationToken);
            await _cache.RemoveAsync("reviews:list:all", "Product deleted via event", cancellationToken);
        }
    }
}
