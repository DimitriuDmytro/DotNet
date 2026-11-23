namespace Bookings.Api.IntegrationEvents.Handlers;

using Bookings.Bll.Caching;
using Bookings.Dal.Interfaces;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Consumer;
using Platform.EventBus.Events;

/// <summary>
/// Обробник події створення послуги у контексті Bookings/Orders (Критерії 9, 10).
/// </summary>
public class ProductCreatedEventHandler : IIntegrationEventHandler<ProductCreatedEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly IBookingsTwoLevelCache? _cache;
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(
        IUnitOfWork uow,
        ILogger<ProductCreatedEventHandler> logger,
        IBookingsTwoLevelCache? cache = null)
    {
        _uow = uow;
        _logger = logger;
        _cache = cache;
    }

    public async Task HandleAsync(ProductCreatedEvent @event, CancellationToken cancellationToken = default)
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

            await _uow.Services.UpsertReplicaAsync(
                @event.ProductId,
                @event.ProductName,
                @event.Price,
                @event.DurationMin,
                @event.OccurredAt,
                cancellationToken);

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Replicated created product for ProductId {ProductId}: '{ProductName}', Price: {Price}",
                @event.ProductId,
                @event.ProductName,
                @event.Price);

            if (_cache != null)
            {
                await _cache.RemoveAsync("services:all", "New product created", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Transaction rolled back due to error processing {EventType} with ID {EventId}",
                nameof(ProductCreatedEvent), @event.EventId);
            throw;
        }
    }
}
