namespace Bookings.Tests.Metrics;

using System.Diagnostics.Metrics;
using Bookings.Api.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class BookingMetricsTests
{
    [Fact]
    public void BookingMetrics_CanBeConstructedAndRecordOperations()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        var sp = services.BuildServiceProvider();
        var meterFactory = sp.GetRequiredService<IMeterFactory>();

        var metrics = new BookingMetrics(meterFactory);

        // Record metrics without exception
        metrics.RecordBookingCreated();
        metrics.RecordStatusChanged("Pending", "Confirmed");
        metrics.RecordStatusChanged("Confirmed", "Cancelled");
        metrics.RecordBookingCancelled();
        metrics.RecordOperationDuration(12.5, "create", "success");
        metrics.SetActiveBookingsCount(5);
    }
}
