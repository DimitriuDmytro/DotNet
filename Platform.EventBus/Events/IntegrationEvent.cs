namespace Platform.EventBus.Events;

/// <summary>
/// Базовий незмінний запис для всіх інтеграційних подій платформи (Критерій 4).
/// </summary>
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}
