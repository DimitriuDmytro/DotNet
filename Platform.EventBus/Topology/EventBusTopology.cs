namespace Platform.EventBus.Topology;

using RabbitMQ.Client;

/// <summary>
/// Декларативна топологія RabbitMQ: Topic Exchanges, черги, bindings та Dead Letter Exchanges/Queues (Критерій 3).
/// </summary>
public static class EventBusTopology
{
    // Exchanges
    public const string CatalogEventsExchange = "catalog-events";
    public const string CatalogEventsDlx = "catalog-events.dlx";
    public const string OrdersEventsExchange = "orders-events";

    // Queues
    public const string OrdersCatalogSyncQueue = "orders.catalog-sync";
    public const string OrdersCatalogSyncDlq = "orders.catalog-sync.dlq";

    public const string BookingsCatalogSyncQueue = "bookings.catalog-sync";
    public const string BookingsCatalogSyncDlq = "bookings.catalog-sync.dlq";

    public const string ReviewsCatalogSyncQueue = "reviews.catalog-sync";
    public const string ReviewsCatalogSyncDlq = "reviews.catalog-sync.dlq";

    // Routing Keys
    public const string ProductWildcardRoutingKey = "product.*";
    public const string ServiceWildcardRoutingKey = "service.*";
    public const string ProductPriceChangedRoutingKey = "product.price_changed";
    public const string ProductRenamedRoutingKey = "product.renamed";
    public const string ProductCreatedRoutingKey = "product.created";
    public const string ProductDeletedRoutingKey = "product.deleted";

    /// <summary>
    /// Ідемпотентне оголошення обмінників, черг, DLX та зв'язувань (Bindings).
    /// </summary>
    public static async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct = default)
    {
        // 1. Оголошення головного Topic Exchange каталогу
        await channel.ExchangeDeclareAsync(
            exchange: CatalogEventsExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        // 2. Оголошення Fanout Dead Letter Exchange (DLX)
        await channel.ExchangeDeclareAsync(
            exchange: CatalogEventsDlx,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        // 3. Оголошення Topic Exchange для подій замовлень
        await channel.ExchangeDeclareAsync(
            exchange: OrdersEventsExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        // 4. Оголошення DLQ для Orders / Bookings та зв'язування з DLX
        await channel.QueueDeclareAsync(
            queue: OrdersCatalogSyncDlq,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: OrdersCatalogSyncDlq,
            exchange: CatalogEventsDlx,
            routingKey: string.Empty,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: BookingsCatalogSyncDlq,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: BookingsCatalogSyncDlq,
            exchange: CatalogEventsDlx,
            routingKey: string.Empty,
            cancellationToken: ct);

        // 5. Оголошення DLQ для Reviews та зв'язування з DLX
        await channel.QueueDeclareAsync(
            queue: ReviewsCatalogSyncDlq,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: ReviewsCatalogSyncDlq,
            exchange: CatalogEventsDlx,
            routingKey: string.Empty,
            cancellationToken: ct);

        // 6. Робоча черга для Orders з аргументом x-dead-letter-exchange
        var ordersQueueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = CatalogEventsDlx
        };

        await channel.QueueDeclareAsync(
            queue: OrdersCatalogSyncQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: ordersQueueArgs,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: OrdersCatalogSyncQueue,
            exchange: CatalogEventsExchange,
            routingKey: ProductWildcardRoutingKey,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: OrdersCatalogSyncQueue,
            exchange: CatalogEventsExchange,
            routingKey: ServiceWildcardRoutingKey,
            cancellationToken: ct);

        // Сумісна черга для Bookings
        await channel.QueueDeclareAsync(
            queue: BookingsCatalogSyncQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: ordersQueueArgs,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: BookingsCatalogSyncQueue,
            exchange: CatalogEventsExchange,
            routingKey: ProductWildcardRoutingKey,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: BookingsCatalogSyncQueue,
            exchange: CatalogEventsExchange,
            routingKey: ServiceWildcardRoutingKey,
            cancellationToken: ct);

        // 7. Робоча черга для Reviews з аргументом x-dead-letter-exchange
        var reviewsQueueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = CatalogEventsDlx
        };

        await channel.QueueDeclareAsync(
            queue: ReviewsCatalogSyncQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: reviewsQueueArgs,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: ReviewsCatalogSyncQueue,
            exchange: CatalogEventsExchange,
            routingKey: ProductWildcardRoutingKey,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: ReviewsCatalogSyncQueue,
            exchange: CatalogEventsExchange,
            routingKey: ProductRenamedRoutingKey,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: ReviewsCatalogSyncQueue,
            exchange: CatalogEventsExchange,
            routingKey: ServiceWildcardRoutingKey,
            cancellationToken: ct);
    }
}
