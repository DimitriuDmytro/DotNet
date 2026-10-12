namespace Bookings.Domain.Models;

/// <summary>
/// Запис на обслуговування (Корінь агрегата Appointment).
/// </summary>
public class Appointment
{
    public long Id { get; set; }
    public long ClientId { get; set; }
    public string Status { get; set; } = "Booked";
    public DateTime ScheduledAt { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[]? RowVersion { get; set; }

    // Навігаційні властивості агрегата
    public AppointmentDetails? Details { get; set; }
    public List<AppointmentItem> Items { get; set; } = [];
}
