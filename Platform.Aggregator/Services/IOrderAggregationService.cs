namespace Platform.Aggregator.Services;

using Platform.Aggregator.Dtos;

public interface IOrderAggregationService
{
    Task<FullOrderDto?> GetFullOrderAsync(long appointmentId, bool noCache, CancellationToken cancellationToken);
    Task<FullOrderDto?> GetFullOrderHttpAsync(long appointmentId, bool noCache, CancellationToken cancellationToken);
}
