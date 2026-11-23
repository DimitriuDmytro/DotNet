namespace Reviews.Tests.Metrics;

using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Reviews.WebApi.Metrics;
using Xunit;

public class ReviewMetricsTests
{
    [Fact]
    public void ReviewMetrics_CanBeConstructedAndRecordOperations()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        var sp = services.BuildServiceProvider();
        var meterFactory = sp.GetRequiredService<IMeterFactory>();

        var metrics = new ReviewMetrics(meterFactory);

        metrics.RecordReviewCreated();
        metrics.RecordReviewDeleted();
        metrics.RecordOperationDuration(15.2, "create", "success");
        metrics.SetTotalReviewsCount(10);
    }
}
