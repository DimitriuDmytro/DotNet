namespace Platform.Aggregator.Services;

public static class AggregatorCacheKeys
{
    public const string Prefix = "aggregator";

    public static string FullOrder(long id) => $"aggregator:order:{id}:full";
}
