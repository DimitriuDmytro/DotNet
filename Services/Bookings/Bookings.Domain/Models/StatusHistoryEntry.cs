namespace Bookings.Domain.Models;

/// <summary>
/// Запис журналу аудиту змін статусів (таблиця dbo.AppointmentStatusHistory).
/// </summary>
public class StatusHistoryEntry
{
    public long Id { get; set; }
    public long AppointmentId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string ChangedBy { get; set; } = "system";
}
