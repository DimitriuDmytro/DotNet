namespace Bookings.Domain.Constants;

/// <summary>
/// Статуси замовлень та таблиця допустимих переходів між статусами (єдине джерело правди в домені).
/// Життєвий цикл:
/// Booked -> CarReceived -> InProgress -> QualityCheck -> Ready -> Delivered
/// Cancelled — дозволено виключно зі статусів Booked та CarReceived.
/// </summary>
public static class AppointmentStatuses
{
    public const string Booked = "Booked";
    public const string CarReceived = "CarReceived";
    public const string InProgress = "InProgress";
    public const string QualityCheck = "QualityCheck";
    public const string Ready = "Ready";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Booked,
        CarReceived,
        InProgress,
        QualityCheck,
        Ready,
        Delivered,
        Cancelled
    };

    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Booked] = new(StringComparer.OrdinalIgnoreCase) { CarReceived, Cancelled },
            [CarReceived] = new(StringComparer.OrdinalIgnoreCase) { InProgress, Cancelled },
            [InProgress] = new(StringComparer.OrdinalIgnoreCase) { QualityCheck },
            [QualityCheck] = new(StringComparer.OrdinalIgnoreCase) { Ready },
            [Ready] = new(StringComparer.OrdinalIgnoreCase) { Delivered },
            [Delivered] = new HashSet<string>(StringComparer.OrdinalIgnoreCase), // Термінальний статус
            [Cancelled] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)  // Термінальний статус
        };

    /// <summary>
    /// Перевіряє, чи дозволено перехід із поточного статусу в новий.
    /// </summary>
    public static bool IsAllowedTransition(string currentStatus, string newStatus)
    {
        if (string.IsNullOrWhiteSpace(currentStatus) || string.IsNullOrWhiteSpace(newStatus))
        {
            return false;
        }

        return AllowedTransitions.TryGetValue(currentStatus, out var nextStatuses) &&
               nextStatuses.Contains(newStatus);
    }

    /// <summary>
    /// Повертає набір допустимих наступних статусів для поточного стану.
    /// </summary>
    public static IReadOnlySet<string> GetAllowedNextStatuses(string currentStatus)
    {
        if (AllowedTransitions.TryGetValue(currentStatus, out var nextStatuses))
        {
            return nextStatuses;
        }

        return new HashSet<string>();
    }

    /// <summary>
    /// Перевіряє, чи дозволено скасування запису з поточного статусу.
    /// </summary>
    public static bool CanBeCancelled(string currentStatus)
    {
        return IsAllowedTransition(currentStatus, Cancelled);
    }
}
