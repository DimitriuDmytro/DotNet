namespace Catalog.Tests.Metrics;

using System.Diagnostics.Metrics;
using Catalog.Api.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class CatalogMetricsTests
{
    [Fact]
    public void CatalogMetrics_CanBeConstructedAndRecordOperations()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        var sp = services.BuildServiceProvider();
        var meterFactory = sp.GetRequiredService<IMeterFactory>();

        var metrics = new CatalogMetrics(meterFactory);

        metrics.RecordServiceCreated();
        metrics.RecordServiceUpdated();
        metrics.RecordPriceChanged();
        metrics.RecordOperationDuration(25.3, "create", "success");
        metrics.SetTotalServicesCount(15);
    }
}
