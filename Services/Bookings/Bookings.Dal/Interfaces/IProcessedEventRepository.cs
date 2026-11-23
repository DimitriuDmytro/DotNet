namespace Bookings.Dal.Interfaces;

/// <summary>
/// Репозиторій для роботи з таблицею dbo.ProcessedEvents для забезпечення ідемпотентності (Критерій 9).
/// </summary>
public interface IProcessedEventRepository
{
    /// <summary>
    /// Спроба зафіксувати подію в таблиці оброблених.
    /// Повертає true, якщо запис успішно додано; false, якщо виник дублікат (помилка 2627/2601).
    /// </summary>
    Task<bool> TryAddAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
}
