namespace Bookings.Bll.Caching;

public static class BookingsCacheKeys
{
    public const string Prefix = "bookings";

    public static string Appointment(long id) => $"bookings:appointment:{id}";

    public static string ClientAppointments(long clientId, string? status = null, int pageNumber = 1, int pageSize = 10)
    {
        var statusPart = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLowerInvariant();
        return $"bookings:client:{clientId}:appointments:s{statusPart}:p{pageNumber}:s{pageSize}";
    }
}

public sealed record CacheEntrySettings(
    TimeSpan L1Ttl,
    TimeSpan L2Ttl,
    TimeSpan? L1Sliding = null,
    int Size = 1);

public static class BookingsCacheTtl
{
    public static readonly CacheEntrySettings Appointment = new(
        L1Ttl: TimeSpan.FromMinutes(5),
        L2Ttl: TimeSpan.FromMinutes(30),
        L1Sliding: TimeSpan.FromMinutes(1),
        Size: 1);

    public static readonly CacheEntrySettings ClientAppointments = new(
        L1Ttl: TimeSpan.FromMinutes(1),
        L2Ttl: TimeSpan.FromMinutes(10),
        L1Sliding: null,
        Size: 2);
}
