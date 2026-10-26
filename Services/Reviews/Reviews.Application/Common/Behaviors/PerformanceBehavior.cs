namespace Reviews.Application.Common.Behaviors;

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Поведінка конвеєра MediatR для відстеження тривалих запитів (Критерій 7, 9).
/// Третя ланка в конвеєрі поведінок.
/// Виводить попередження (Warning) у лог, якщо виконання запиту перевищило поріг 500 мс.
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int WarningThresholdMilliseconds = 500;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();
        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        if (elapsedMilliseconds > WarningThresholdMilliseconds)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogWarning(
                "Тривалий запит: {RequestName} тривав {ElapsedMilliseconds} мс (поріг: {Threshold} мс) {@Request}",
                requestName,
                elapsedMilliseconds,
                WarningThresholdMilliseconds,
                request);
        }

        return response;
    }
}
