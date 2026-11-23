namespace Reviews.Domain.Interfaces;

/// <summary>
/// Інтерфейс для перевірки та фіксації ідемпотентності оброблених подій у MongoDB (Критерій 9).
/// </summary>
public interface IProcessedEventTracker
{
    /// <summary>
    /// Спроба зберегти EventId у колекції processedEvents.
    /// Повертає true, якщо запис успішно додано вперше; false, якщо виник дублікат (код 11000).
    /// </summary>
    Task<bool> TryTrackAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
}
