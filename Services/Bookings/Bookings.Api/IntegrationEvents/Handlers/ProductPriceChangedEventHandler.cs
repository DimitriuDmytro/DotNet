namespace Bookings.Api.IntegrationEvents.Handlers;

using Bookings.Bll.Caching;
using Bookings.Dal.Interfaces;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Consumer;
using Platform.EventBus.Events;

/// <summary>
/// Обробник події зміни ціни послуги у контексті Bookings/Orders (Критерії 9, 10).
/// Виконує ідемпотентну фіксацію EventId та оновлення репліки в межах єдиної транзакції.
/// Зберігає історичний зліпок UnitPrice у таблиці AppointmentItems незмінним.
/// </summary>
public class ProductPriceChangedEventHandler : IIntegrationEventHandler<ProductPriceChangedEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly IBookingsTwoLevelCache? _cache;
    private readonly ILogger<ProductPriceChangedEventHandler> _logger;

    public ProductPriceChangedEventHandler(
        IUnitOfWork uow,
        ILogger<ProductPriceChangedEventHandler> logger,
        IBookingsTwoLevelCache? cache = null)
    {
        _uow = uow;
        _logger = logger;
        _cache = cache;
    }

    public async Task HandleAsync(ProductPriceChangedEvent @event, CancellationToken cancellationToken = default)
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

            // Оновлюємо ціну репліки dbo.Services з контролем OccurredAt.
            // AppointmentItems.UnitPrice залишається незмінним.
            await _uow.Services.UpdatePriceAsync(@event.ProductId, @event.NewPrice, @event.OccurredAt, cancellationToken);
            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Replicated price change for ProductId {ProductId}: {OldPrice} -> {NewPrice}",
                @event.ProductId,
                @event.OldPrice,
                @event.NewPrice);

            if (_cache != null)
            {
                await _cache.RemoveAsync($"service:{@event.ProductId}", "Price changed via event", cancellationToken);
                await _cache.RemoveAsync("services:all", "Price changed via event", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Transaction rolled back due to error processing {EventType} with ID {EventId}",
                nameof(ProductPriceChangedEvent), @event.EventId);
            throw;
        }
    }
}
