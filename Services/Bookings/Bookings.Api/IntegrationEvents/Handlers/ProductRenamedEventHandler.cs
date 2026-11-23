namespace Bookings.Api.IntegrationEvents.Handlers;

using Bookings.Bll.Caching;
using Bookings.Dal.Interfaces;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Consumer;
using Platform.EventBus.Events;

/// <summary>
/// Обробник події перейменування послуги у контексті Bookings/Orders (Критерії 9, 10).
/// </summary>
public class ProductRenamedEventHandler : IIntegrationEventHandler<ProductRenamedEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly IBookingsTwoLevelCache? _cache;
    private readonly ILogger<ProductRenamedEventHandler> _logger;

    public ProductRenamedEventHandler(
        IUnitOfWork uow,
        ILogger<ProductRenamedEventHandler> logger,
        IBookingsTwoLevelCache? cache = null)
    {
        _uow = uow;
        _logger = logger;
        _cache = cache;
    }

    public async Task HandleAsync(ProductRenamedEvent @event, CancellationToken cancellationToken = default)
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

            await _uow.Services.UpdateNameAsync(@event.ProductId, @event.NewName, @event.OccurredAt, cancellationToken);
            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Replicated renamed product for ProductId {ProductId}: '{OldName}' -> '{NewName}'",
                @event.ProductId,
                @event.OldName,
                @event.NewName);

            if (_cache != null)
            {
                await _cache.RemoveAsync($"service:{@event.ProductId}", "Name changed via event", cancellationToken);
                await _cache.RemoveAsync("services:all", "Name changed via event", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Transaction rolled back due to error processing {EventType} with ID {EventId}",
                nameof(ProductRenamedEvent), @event.EventId);
            throw;
        }
    }
}
