namespace Reviews.WebApi.IntegrationEvents.Events;

using Platform.EventBus.Events;

/// <summary>
/// Локальні контракти інтеграційних подій каталогу для мікросервісу Reviews (Критерій 4).
/// </summary>
public record ProductRenamedEvent : Platform.EventBus.Events.ProductRenamedEvent
{
    public ProductRenamedEvent() { }
    public ProductRenamedEvent(long productId, string oldName, string newName, DateTime? occurredAt = null, string? correlationId = null)
        : base(productId, oldName, newName, occurredAt, correlationId) { }
}

public record ProductPriceChangedEvent : Platform.EventBus.Events.ProductPriceChangedEvent
{
    public ProductPriceChangedEvent() { }
    public ProductPriceChangedEvent(long productId, decimal oldPrice, decimal newPrice, DateTime? occurredAt = null, string? correlationId = null)
        : base(productId, oldPrice, newPrice, occurredAt, correlationId) { }
    public ProductPriceChangedEvent(long productId, string productName, decimal oldPrice, decimal newPrice, DateTime? occurredAt = null, string? correlationId = null)
        : base(productId, productName, oldPrice, newPrice, occurredAt, correlationId) { }
}

public record ProductCreatedEvent : Platform.EventBus.Events.ProductCreatedEvent
{
    public ProductCreatedEvent() { }
    public ProductCreatedEvent(long productId, string productName, decimal price, int durationMin = 0, DateTime? occurredAt = null, string? correlationId = null)
        : base(productId, productName, price, durationMin, occurredAt, correlationId) { }
}

public record ProductDeletedEvent : Platform.EventBus.Events.ProductDeletedEvent
{
    public ProductDeletedEvent() { }
    public ProductDeletedEvent(long productId, string productName, DateTime? occurredAt = null, string? correlationId = null)
        : base(productId, productName, occurredAt, correlationId) { }
}
