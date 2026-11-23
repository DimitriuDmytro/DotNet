namespace Platform.EventBus.Publisher;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Platform.EventBus.Metrics;
using Platform.EventBus.Serialization;
using Platform.EventBus.Tracing;
using RabbitMQ.Client;

/// <summary>
/// Реалізація публікатора подій через RabbitMQ 7.x асинхронний IChannel (Критерій 5, 12).
/// Підтримує W3C Trace Context (ActivityKind.Producer), CorrelationId та персистентні повідомлення.
/// </summary>
public sealed class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(IServiceProvider serviceProvider, ILogger<RabbitMqEventPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task PublishAsync<T>(
        string exchange,
        string routingKey,
        T @event,
        CancellationToken ct = default) where T : IntegrationEvent
    {
        return PublishAsync(@event, exchange, routingKey, ct);
    }

    public async Task PublishAsync<T>(
        T @event,
        string exchange,
        string routingKey,
        CancellationToken ct = default) where T : IntegrationEvent
    {
        IConnection? connection = null;
        try
        {
            connection = _serviceProvider.GetService<IConnection>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not resolve RabbitMQ connection for event {EventType}", typeof(T).Name);
            return;
        }

        if (connection == null)
        {
            _logger.LogWarning("RabbitMQ connection is unavailable. Skipping event publishing in offline/test environment");
            return;
        }

        // Критерій 12: Запуск Producer Activity навколо відправки повідомлення
        using var activity = TracePropagation.Source.StartActivity($"{exchange} publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag("messaging.system", "rabbitmq");
            activity.SetTag("messaging.destination.name", exchange);
            activity.SetTag("messaging.destination.routing_key", routingKey);
            activity.SetTag("messaging.message.id", @event.EventId.ToString());
            activity.SetTag("messaging.operation", "publish");
        }

        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        var correlationId = @event.CorrelationId
            ?? Activity.Current?.GetTagItem("correlation.id")?.ToString()
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString();

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = @event.EventId.ToString(),
            Type = typeof(T).Name,
            CorrelationId = correlationId
        };

        props.Headers = new Dictionary<string, object?>
        {
            ["x-correlation-id"] = correlationId
        };

        TracePropagation.Inject(props);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, JsonDefaults.Options);

        _logger.LogInformation(
            "Publishing integration event {EventType} with ID {EventId} to exchange {Exchange} with routing key {RoutingKey}. CorrelationId: {CorrelationId}",
            typeof(T).Name,
            @event.EventId,
            exchange,
            routingKey,
            correlationId);

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);

        EventBusMetrics.RecordPublished(typeof(T).Name, exchange);
    }
}
