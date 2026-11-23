namespace Reviews.WebApi.IntegrationEvents.Handlers;

using MediatR;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Consumer;
using Platform.EventBus.Events;
using Reviews.Application.Caching;
using Reviews.Application.Reviews.Commands.UpdateServiceNameInReviews;
using Reviews.Domain.Interfaces;

/// <summary>
/// Обробник події перейменування послуги у контексті Reviews (Критерії 9, 10).
/// Забезпечує ідемпотентну обробку через колекцію processedEvents та диспетчеризацію MediatR-команди.
/// </summary>
public class ProductRenamedEventHandler : IIntegrationEventHandler<ProductRenamedEvent>
{
    private readonly IProcessedEventTracker _tracker;
    private readonly IMediator _mediator;
    private readonly IReviewsTwoLevelCache? _cache;
    private readonly ILogger<ProductRenamedEventHandler> _logger;

    public ProductRenamedEventHandler(
        IProcessedEventTracker tracker,
        IMediator mediator,
        ILogger<ProductRenamedEventHandler> logger,
        IReviewsTwoLevelCache? cache = null)
    {
        _tracker = tracker;
        _mediator = mediator;
        _logger = logger;
        _cache = cache;
    }

    public async Task HandleAsync(ProductRenamedEvent @event, CancellationToken cancellationToken = default)
    {
        var isNew = await _tracker.TryTrackAsync(@event.EventId, cancellationToken);
        if (!isNew)
        {
            _logger.LogInformation("Duplicate event {EventId} skipped", @event.EventId);
            return;
        }

        await _mediator.Send(
            new UpdateServiceNameInReviewsCommand(@event.ProductId, @event.NewName, @event.OccurredAt),
            cancellationToken);

        _logger.LogInformation(
            "Dispatched UpdateServiceNameInReviewsCommand for ProductId {ProductId}: '{OldName}' -> '{NewName}'",
            @event.ProductId,
            @event.OldName,
            @event.NewName);
    }
}
