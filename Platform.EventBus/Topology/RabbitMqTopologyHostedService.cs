namespace Platform.EventBus.Topology;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

/// <summary>
/// Фонова ініціалізація декларативної топології RabbitMQ під час запуску мікросервісу (Критерій 3).
/// Безпечно обробляє відсутність брокера у тестовому середовищі без блокування запуску хоста.
/// </summary>
public sealed class RabbitMqTopologyHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqTopologyHostedService> _logger;

    public RabbitMqTopologyHostedService(IServiceProvider serviceProvider, ILogger<RabbitMqTopologyHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Не блокуємо запуск хоста
        await Task.Yield();

        try
        {
            var connection = _serviceProvider.GetService<IConnection>();
            if (connection == null)
            {
                _logger.LogWarning("IConnection не зареєстровано або брокер недоступний у поточному середовищі");
                return;
            }

            await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await EventBusTopology.DeclareTopologyAsync(channel, stoppingToken);
            _logger.LogInformation("RabbitMQ topology declared successfully (Exchanges, Queues, DLX, DLQ, Bindings)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Помилка або очікування готовності брокера при ініціалізації топології RabbitMQ");
        }
    }
}
