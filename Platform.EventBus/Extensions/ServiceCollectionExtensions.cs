namespace Microsoft.Extensions.DependencyInjection;

using Platform.EventBus.Publisher;
using Platform.EventBus.Topology;

public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// Реєструє IEventPublisher для відправки подій через RabbitMQ та ініціалізатор топології.
    /// </summary>
    public static IServiceCollection AddEventBusPublisher(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddHostedService<RabbitMqTopologyHostedService>();
        return services;
    }
}
