namespace Platform.EventBus.Consumer;

using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

/// <summary>
/// Допоміжний клас для обробки повторів через x-retry-count та відправки у Dead Letter Queue (Критерій 11).
/// </summary>
public static class DeadLetterHelper
{
    public const string RetryHeaderName = "x-retry-count";
    public const int DefaultMaxRetries = 3;

    public static Task RetryOrDeadLetterAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        Exception ex,
        ILogger logger,
        CancellationToken ct)
    {
        return RetryOrDeadLetterAsync(channel, ea, ex, logger, DefaultMaxRetries, ct);
    }

    public static async Task RetryOrDeadLetterAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        Exception ex,
        ILogger logger,
        int maxRetries = DefaultMaxRetries,
        CancellationToken ct = default)
    {
        var retryCount = GetRetryCount(ea.BasicProperties.Headers);
        var eventType = ea.BasicProperties.Type ?? "UnknownEvent";
        var messageId = ea.BasicProperties.MessageId ?? Guid.Empty.ToString();

        if (retryCount >= maxRetries)
        {
            logger.LogError(
                ex,
                "Max retries ({MaxRetries}) reached for event {EventType} (MessageId: {MessageId}). Rejecting message to DLQ without requeue",
                maxRetries,
                eventType,
                messageId);

            // Nack без requeue пересилає повідомлення у x-dead-letter-exchange
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
            return;
        }

        var nextRetry = retryCount + 1;
        logger.LogWarning(
            ex,
            "Transient failure processing event {EventType} (MessageId: {MessageId}). Scheduling retry attempt {Attempt}/{MaxRetries}",
            eventType,
            messageId,
            nextRetry,
            maxRetries);

        var headers = ea.BasicProperties.Headers != null
            ? new Dictionary<string, object?>(ea.BasicProperties.Headers)
            : new Dictionary<string, object?>();

        headers[RetryHeaderName] = nextRetry;

        var retryProps = new BasicProperties
        {
            Persistent = true,
            ContentType = ea.BasicProperties.ContentType,
            CorrelationId = ea.BasicProperties.CorrelationId,
            MessageId = ea.BasicProperties.MessageId,
            Type = ea.BasicProperties.Type,
            Headers = headers
        };

        // Публікуємо повторно в ту саму точку обміну або чергу
        await channel.BasicPublishAsync(
            exchange: ea.Exchange,
            routingKey: ea.RoutingKey,
            mandatory: false,
            basicProperties: retryProps,
            body: ea.Body,
            cancellationToken: ct);

        // Підтверджуємо оригінальне повідомлення
        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
    }

    public static int GetRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers == null || !headers.TryGetValue(RetryHeaderName, out var rawVal) || rawVal == null)
        {
            return 0;
        }

        return rawVal switch
        {
            int i => i,
            long l => (int)l,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => 0
        };
    }
}
