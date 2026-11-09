namespace Bookings.Bll.Caching;

using Bookings.Bll.Interfaces;
using Bookings.Bll.Services;
using Microsoft.Extensions.DependencyInjection;

public static class BookingsCachingExtensions
{
    public static IServiceCollection AddBookingsCaching(this IServiceCollection services)
    {
        services.AddSingleton<BookingsCacheMetrics>();
        services.AddSingleton<IBookingsTwoLevelCache, BookingsTwoLevelCache>();
        services.AddTransient<BookingsCacheHealthCheck>();

        services.AddScoped<AppointmentService>();
        services.AddScoped<IAppointmentService, CachedAppointmentService>();

        return services;
    }
}
