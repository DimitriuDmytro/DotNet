namespace Reviews.WebApi.IntegrationEvents;

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Consumer;
using Platform.EventBus.Events;
using Platform.EventBus.Serialization;
using Platform.EventBus.Topology;
using Platform.EventBus.Tracing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Reviews.Application.Caching;
using Reviews.Application.Reviews.Commands.UpdateServiceNameInReviews;
using Reviews.Domain.Interfaces;

/// <summary>
/// Фоновий споживач подій каталогу для оновлення денормалізованих даних у MongoDB (Критерії 7, 8, 9, 10, 11, 12, 13).
/// Працює через RabbitMQ 7.x AsyncEventingBasicConsumer з ручним підтвердженням (autoAck: false).
/// </summary>
public class CatalogEventsConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CatalogEventsConsumer> _logger;

    public CatalogEventsConsumer(
        IServiceProvider serviceProvider,
        ILogger<CatalogEventsConsumer> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        IConnection? connection = null;
        try
        {
            connection = _serviceProvider.GetService<IConnection>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot connect to RabbitMQ in Reviews CatalogEventsConsumer. Host running in test/offline mode.");
            return;
        }

        if (connection == null)
        {
            _logger.LogWarning("RabbitMQ connection is unavailable. Reviews CatalogEventsConsumer will not start.");
            return;
        }

        IChannel channel;
        try
        {
            channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create RabbitMQ channel in Reviews CatalogEventsConsumer.");
            return;
        }

        // Критерій 3 & 7: Декларативна топологія та налаштування PrefetchCount
        await EventBusTopology.DeclareTopologyAsync(channel, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            await ProcessMessageAsync(channel, ea, stoppingToken);
        };

        // Підписка на чергу reviews.catalog-sync (з DLX catalog-events.dlx)
        await channel.BasicConsumeAsync(
            queue: EventBusTopology.ReviewsCatalogSyncQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Reviews CatalogEventsConsumer successfully subscribed to queue {Queue} with prefetchCount 10",
            EventBusTopology.ReviewsCatalogSyncQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Reviews CatalogEventsConsumer stopping");
        }
        finally
        {
            await channel.CloseAsync(cancellationToken: CancellationToken.None);
        }
    }

    private async Task ProcessMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        // Критерій 12: Вилучення W3C traceparent та контексту трасування OpenTelemetry
        using var activity = TracePropagation.Extract(ea.BasicProperties, "Reviews.CatalogEventsConsumer.Process");
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", ea.Exchange);
        activity?.SetTag("messaging.routing_key", ea.RoutingKey);
        activity?.SetTag("messaging.message.id", ea.BasicProperties?.MessageId);

        var correlationId = TracePropagation.GetCorrelationId(ea.BasicProperties) ?? Guid.NewGuid().ToString();
        activity?.SetTag("correlation.id", correlationId);

        var messageType = ea.BasicProperties?.Type;
        var messageIdStr = ea.BasicProperties?.MessageId;
        Guid.TryParse(messageIdStr, out var eventId);

        // Критерій 8: Перевірка на невідновлювані помилки валідації та десеріалізації
        try
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            IntegrationEvent? integrationEvent = null;
            if (messageType == nameof(ProductRenamedEvent) || ea.RoutingKey == EventBusTopology.ProductRenamedRoutingKey)
            {
                integrationEvent = JsonSerializer.Deserialize<ProductRenamedEvent>(json, JsonDefaults.Options);
            }
            else if (messageType == nameof(ProductPriceChangedEvent) || ea.RoutingKey == EventBusTopology.ProductPriceChangedRoutingKey)
            {
                integrationEvent = JsonSerializer.Deserialize<ProductPriceChangedEvent>(json, JsonDefaults.Options);
            }
            else if (messageType == nameof(ProductCreatedEvent) || ea.RoutingKey == EventBusTopology.ProductCreatedRoutingKey)
            {
                integrationEvent = JsonSerializer.Deserialize<ProductCreatedEvent>(json, JsonDefaults.Options);
            }
            else if (messageType == nameof(ProductDeletedEvent) || ea.RoutingKey == EventBusTopology.ProductDeletedRoutingKey)
            {
                integrationEvent = JsonSerializer.Deserialize<ProductDeletedEvent>(json, JsonDefaults.Options);
            }
            else
            {
                // Невідомий тип повідомлення -> невідновлювана помилка -> NACK без повторної черги (DLQ)
                _logger.LogError(
                    "Permanent failure: Unknown message type {EventType} with MessageId {MessageId}. Routing to DLQ.",
                    messageType,
                    messageIdStr);

                await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                return;
            }

            if (integrationEvent == null)
            {
                _logger.LogError("Permanent failure: Deserialized event is null for MessageId {MessageId}. Routing to DLQ.", messageIdStr);
                await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                return;
            }

            if (eventId == Guid.Empty)
            {
                eventId = integrationEvent.EventId;
            }

            // Окремий DI Scope на кожне повідомлення (Критерій 7)
            using var scope = _serviceProvider.CreateScope();
            var tracker = scope.ServiceProvider.GetRequiredService<IProcessedEventTracker>();

            // Критерій 9: Ідемпотентна обробка через колекцію processedEvents
            if (eventId != Guid.Empty)
            {
                var isNew = await tracker.TryTrackAsync(eventId, stoppingToken);
                if (!isNew)
                {
                    _logger.LogWarning(
                        "Duplicate event detected: EventId {EventId}, Type {EventType}. Skipping duplicate and ACKing.",
                        eventId,
                        messageType);

                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    return;
                }
            }

            // Критерій 10: Реплікація денормалізованих даних у MongoDB через MediatR
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var cache = scope.ServiceProvider.GetService<IReviewsTwoLevelCache>();

            switch (integrationEvent)
            {
                case ProductRenamedEvent renameEvent:
                    await mediator.Send(
                        new UpdateServiceNameInReviewsCommand(renameEvent.ProductId, renameEvent.NewName, renameEvent.OccurredAt),
                        stoppingToken);

                    _logger.LogInformation(
                        "Dispatched UpdateServiceNameInReviewsCommand for ProductId {ProductId}: '{OldName}' -> '{NewName}'. CorrelationId: {CorrelationId}",
                        renameEvent.ProductId,
                        renameEvent.OldName,
                        renameEvent.NewName,
                        correlationId);
                    break;

                case ProductDeletedEvent deletedEvent:
                    _logger.LogInformation(
                        "Received product deleted event for ProductId {ProductId}. CorrelationId: {CorrelationId}",
                        deletedEvent.ProductId,
                        correlationId);

                    if (cache != null)
                    {
                        await cache.RemoveAsync($"reviews:service:{deletedEvent.ProductId}", "Product deleted via event", stoppingToken);
                        await cache.RemoveAsync("reviews:list:all", "Product deleted via event", stoppingToken);
                    }
                    break;

                case ProductCreatedEvent createdEvent:
                    _logger.LogInformation(
                        "Received product created event for ProductId {ProductId}: '{ProductName}'. CorrelationId: {CorrelationId}",
                        createdEvent.ProductId,
                        createdEvent.ProductName,
                        correlationId);
                    break;

                case ProductPriceChangedEvent priceEvent:
                    _logger.LogInformation(
                        "Received product price changed event for ProductId {ProductId}: {OldPrice} -> {NewPrice}. CorrelationId: {CorrelationId}",
                        priceEvent.ProductId,
                        priceEvent.OldPrice,
                        priceEvent.NewPrice,
                        correlationId);
                    break;
            }

            // Успішне підтвердження ACK
            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            _logger.LogInformation(
                "Event {EventType} with ID {EventId} processed and ACKed successfully",
                messageType,
                eventId);
        }
        catch (JsonException jsonEx)
        {
            // Невідновлювана помилка формату JSON -> NACK без повтору -> відправка у DLQ (Критерій 8)
            _logger.LogError(
                jsonEx,
                "Permanent JSON deserialization error for MessageId {MessageId}. NACKing to DLQ.",
                messageIdStr);

            await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            // Відновлювана помилка -> повтор із заголовком x-retry-count або Dead Letter після max retries (Критерій 11)
            _logger.LogError(
                ex,
                "Transient error while processing event {EventType} with ID {EventId}. Initiating retry policy.",
                messageType,
                eventId);

            await DeadLetterHelper.RetryOrDeadLetterAsync(channel, ea, ex, _logger, stoppingToken);
        }
    }
}
