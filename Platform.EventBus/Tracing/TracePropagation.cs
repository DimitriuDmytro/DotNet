namespace Platform.EventBus.Tracing;

using System.Diagnostics;
using System.Text;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

/// <summary>
/// Передача W3C контексту розподіленого трасування (traceparent) та CorrelationId через брокер повідомлень (Критерій 12).
/// </summary>
public static class TracePropagation
{
    public static readonly ActivitySource Source = new("Platform.Messaging");
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public static void Inject(BasicProperties props)
    {
        var currentActivity = Activity.Current;
        if (currentActivity is null)
        {
            return;
        }

        props.Headers ??= new Dictionary<string, object?>();

        Propagator.Inject(
            new PropagationContext(currentActivity.Context, default),
            props.Headers,
            (headers, key, value) => headers[key] = Encoding.UTF8.GetBytes(value));
    }

    public static Activity? Extract(IReadOnlyBasicProperties? props, string operationName = "Message.Process")
    {
        var parentContext = Propagator.Extract(
            default,
            props?.Headers,
            (headers, key) =>
            {
                if (headers != null && headers.TryGetValue(key, out var rawVal) && rawVal != null)
                {
                    return rawVal switch
                    {
                        byte[] bytes => [Encoding.UTF8.GetString(bytes)],
                        string str => [str],
                        _ => []
                    };
                }
                return [];
            });

        var activity = Source.StartActivity(
            operationName,
            ActivityKind.Consumer,
            parentContext.ActivityContext);

        if (activity != null && props != null)
        {
            activity.SetTag("messaging.system", "rabbitmq");
            if (!string.IsNullOrEmpty(props.MessageId))
            {
                activity.SetTag("messaging.message.id", props.MessageId);
            }
        }

        return activity;
    }

    public static Activity? StartConsumerActivity(BasicDeliverEventArgs ea, string queue)
    {
        var activity = Extract(ea.BasicProperties, $"{queue} process");
        if (activity != null)
        {
            activity.SetTag("messaging.system", "rabbitmq");
            activity.SetTag("messaging.destination.name", ea.Exchange);
            activity.SetTag("messaging.destination.routing_key", ea.RoutingKey);
            activity.SetTag("messaging.rabbitmq.queue", queue);
            activity.SetTag("messaging.message.id", ea.BasicProperties.MessageId);
        }

        return activity;
    }

    public static string? GetCorrelationId(IReadOnlyBasicProperties? props)
    {
        if (props?.Headers != null &&
            props.Headers.TryGetValue("x-correlation-id", out var raw) &&
            raw != null)
        {
            return raw switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string s => s,
                _ => raw.ToString()
            };
        }
        return props?.CorrelationId;
    }

    public static string? ExtractCorrelationId(BasicDeliverEventArgs ea)
    {
        return GetCorrelationId(ea.BasicProperties);
    }
}
