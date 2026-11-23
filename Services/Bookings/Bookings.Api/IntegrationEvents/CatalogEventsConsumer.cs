namespace Bookings.Api.IntegrationEvents;

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Bookings.Api.IntegrationEvents.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Consumer;
using Platform.EventBus.Events;
using Platform.EventBus.Metrics;
using Platform.EventBus.Serialization;
using Platform.EventBus.Topology;
using Platform.EventBus.Tracing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

/// <summary>
/// Фоновий споживач подій каталогу для оновлення локальної репліки dbo.Services (Критерії 7, 8, 9, 10, 11, 12, 13).
/// Працює через RabbitMQ 7.x AsyncEventingBasicConsumer з ручним підтвердженням (autoAck: false).
/// Делегує обробку виділеним класам-обробникам IIntegrationEventHandler.
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
            _logger.LogWarning(ex, "Cannot connect to RabbitMQ in Bookings CatalogEventsConsumer. Host running in test/offline mode.");
            return;
        }

        if (connection == null)
        {
            _logger.LogWarning("RabbitMQ connection is unavailable. Bookings CatalogEventsConsumer will not start.");
            return;
        }

        IChannel channel;
        try
        {
            channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create RabbitMQ channel in Bookings CatalogEventsConsumer.");
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

        // Підписка на черги orders.catalog-sync та bookings.catalog-sync
        await channel.BasicConsumeAsync(
            queue: EventBusTopology.OrdersCatalogSyncQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await channel.BasicConsumeAsync(
            queue: EventBusTopology.BookingsCatalogSyncQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Bookings CatalogEventsConsumer successfully subscribed to queues with prefetchCount 10");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CatalogEventsConsumer stopping");
        }
        finally
        {
            await channel.CloseAsync(cancellationToken: CancellationToken.None);
        }
    }

    private async Task ProcessMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        // Критерій 12: Вилучення W3C traceparent та контексту трасування OpenTelemetry
        using var activity = TracePropagation.Extract(ea.BasicProperties, "Bookings.CatalogEventsConsumer.Process");
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", ea.Exchange);
        activity?.SetTag("messaging.routing_key", ea.RoutingKey);
        activity?.SetTag("messaging.message.id", ea.BasicProperties?.MessageId);

        var correlationId = TracePropagation.GetCorrelationId(ea.BasicProperties) ?? Guid.NewGuid().ToString();
        activity?.SetTag("correlation.id", correlationId);

        var messageType = ea.BasicProperties?.Type;
        var messageIdStr = ea.BasicProperties?.MessageId;
        Guid.TryParse(messageIdStr, out var eventId);

        // Критерій 12: Logging scope збагачує всі логи контекстом
        using var logScope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["EventId"] = eventId,
            ["EventType"] = messageType,
            ["Queue"] = ea.ConsumerTag
        });

        // Критерій 8: Перевірка на невідновлювані помилки валідації та десеріалізації
        try
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            IntegrationEvent? integrationEvent = null;
            if (messageType == nameof(ProductPriceChangedEvent) || ea.RoutingKey == EventBusTopology.ProductPriceChangedRoutingKey)
            {
                integrationEvent = JsonSerializer.Deserialize<ProductPriceChangedEvent>(json, JsonDefaults.Options);
            }
            else if (messageType == nameof(ProductRenamedEvent) || ea.RoutingKey == EventBusTopology.ProductRenamedRoutingKey)
            {
                integrationEvent = JsonSerializer.Deserialize<ProductRenamedEvent>(json, JsonDefaults.Options);
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
                _logger.LogError(
                    "Permanent failure: Unknown message type {EventType} with MessageId {MessageId}. Routing to DLQ.",
                    messageType,
                    messageIdStr);

                EventBusMetrics.RecordFailed(messageType ?? "Unknown", EventBusTopology.OrdersCatalogSyncQueue, "UnknownType");
                await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                return;
            }

            if (integrationEvent == null)
            {
                _logger.LogError("Permanent failure: Deserialized event is null for MessageId {MessageId}. Routing to DLQ.", messageIdStr);
                EventBusMetrics.RecordFailed(messageType ?? "Unknown", EventBusTopology.OrdersCatalogSyncQueue, "NullEvent");
                await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                return;
            }

            if (eventId == Guid.Empty)
            {
                eventId = integrationEvent.EventId;
            }

            // Окремий DI Scope на кожне повідомлення (Критерій 7)
            using var scope = _serviceProvider.CreateScope();

            // Критерій 9 & 10: Делегування обробки виділеному класу-обробнику
            switch (integrationEvent)
            {
                case ProductPriceChangedEvent priceEvent:
                    var priceHandler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<ProductPriceChangedEvent>>();
                    await priceHandler.HandleAsync(priceEvent, stoppingToken);
                    break;

                case ProductRenamedEvent renameEvent:
                    var renameHandler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<ProductRenamedEvent>>();
                    await renameHandler.HandleAsync(renameEvent, stoppingToken);
                    break;

                case ProductCreatedEvent createdEvent:
                    var createdHandler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<ProductCreatedEvent>>();
                    await createdHandler.HandleAsync(createdEvent, stoppingToken);
                    break;

                case ProductDeletedEvent deletedEvent:
                    var deletedHandler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<ProductDeletedEvent>>();
                    await deletedHandler.HandleAsync(deletedEvent, stoppingToken);
                    break;
            }

            // Успішне підтвердження ACK
            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            EventBusMetrics.RecordConsumed(messageType ?? "Unknown", EventBusTopology.OrdersCatalogSyncQueue, "Success");

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

            EventBusMetrics.RecordFailed(messageType ?? "JsonError", EventBusTopology.OrdersCatalogSyncQueue, "JsonException");
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

            EventBusMetrics.RecordFailed(messageType ?? "Transient", EventBusTopology.OrdersCatalogSyncQueue, "TransientRetry");
            await DeadLetterHelper.RetryOrDeadLetterAsync(channel, ea, ex, _logger, stoppingToken);
        }
    }
}
