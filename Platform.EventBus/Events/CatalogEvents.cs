namespace Platform.EventBus.Events;

/// <summary>
/// Подія зміни ціни товару/послуги (Критерій 4).
/// </summary>
public record ProductPriceChangedEvent : IntegrationEvent
{
    public long ProductId { get; init; }
    public string? ProductName { get; init; }
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }

    public ProductPriceChangedEvent() { }

    public ProductPriceChangedEvent(long productId, decimal oldPrice, decimal newPrice, DateTime? occurredAt = null, string? correlationId = null)
    {
        ProductId = productId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        if (occurredAt.HasValue) OccurredAt = occurredAt.Value;
        CorrelationId = correlationId;
    }

    public ProductPriceChangedEvent(long productId, string productName, decimal oldPrice, decimal newPrice, DateTime? occurredAt = null, string? correlationId = null)
        : this(productId, oldPrice, newPrice, occurredAt, correlationId)
    {
        ProductName = productName;
    }
}

/// <summary>
/// Подія перейменування товару/послуги (Критерій 4).
/// </summary>
public record ProductRenamedEvent : IntegrationEvent
{
    public long ProductId { get; init; }
    public string OldName { get; init; } = string.Empty;
    public string NewName { get; init; } = string.Empty;

    public ProductRenamedEvent() { }

    public ProductRenamedEvent(long productId, string oldName, string newName, DateTime? occurredAt = null, string? correlationId = null)
    {
        ProductId = productId;
        OldName = oldName;
        NewName = newName;
        if (occurredAt.HasValue) OccurredAt = occurredAt.Value;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Подія створення нового товару/послуги в каталозі (Критерій 4).
/// </summary>
public record ProductCreatedEvent : IntegrationEvent
{
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int DurationMin { get; init; }

    public ProductCreatedEvent() { }

    public ProductCreatedEvent(long productId, string productName, decimal price, int durationMin = 0, DateTime? occurredAt = null, string? correlationId = null)
    {
        ProductId = productId;
        ProductName = productName;
        Price = price;
        DurationMin = durationMin;
        if (occurredAt.HasValue) OccurredAt = occurredAt.Value;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Подія видалення товару/послуги з каталогу (Критерій 4).
/// </summary>
public record ProductDeletedEvent : IntegrationEvent
{
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;

    public ProductDeletedEvent() { }

    public ProductDeletedEvent(long productId, string productName, DateTime? occurredAt = null, string? correlationId = null)
    {
        ProductId = productId;
        ProductName = productName;
        if (occurredAt.HasValue) OccurredAt = occurredAt.Value;
        CorrelationId = correlationId;
    }
}
