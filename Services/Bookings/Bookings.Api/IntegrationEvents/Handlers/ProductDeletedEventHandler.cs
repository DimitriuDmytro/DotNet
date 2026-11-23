namespace Bookings.Api.IntegrationEvents.Handlers;

using Bookings.Bll.Caching;
using Bookings.Dal.Interfaces;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Consumer;
using Platform.EventBus.Events;

/// <summary>
/// Обробник події видалення послуги у контексті Bookings/Orders (Критерії 9, 10).
/// </summary>
public class ProductDeletedEventHandler : IIntegrationEventHandler<ProductDeletedEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly IBookingsTwoLevelCache? _cache;
    private readonly ILogger<ProductDeletedEventHandler> _logger;

    public ProductDeletedEventHandler(
        IUnitOfWork uow,
        ILogger<ProductDeletedEventHandler> logger,
        IBookingsTwoLevelCache? cache = null)
    {
        _uow = uow;
        _logger = logger;
        _cache = cache;
    }

    public async Task HandleAsync(ProductDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var isNew = await _uow.ProcessedEvents.TryAddAsync(@event.EventId, cancellationToken);
            if (!isNew)
            {
                await _uow.RollbackAsync(cancellationToken);
                _logger.LogInformation("Duplicate event {EventId} skipped", @event.EventId);
                return;
            }

            // Позначаємо у локальній копії або очищуємо кеш
            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("Processed product deleted event for ProductId {ProductId}", @event.ProductId);

            if (_cache != null)
            {
                await _cache.RemoveAsync($"service:{@event.ProductId}", "Product deleted via event", cancellationToken);
                await _cache.RemoveAsync("services:all", "Product deleted via event", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Transaction rolled back due to error processing {EventType} with ID {EventId}",
                nameof(ProductDeletedEvent), @event.EventId);
            throw;
        }
    }
}
