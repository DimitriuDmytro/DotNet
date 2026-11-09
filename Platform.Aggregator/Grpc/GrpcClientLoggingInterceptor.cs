namespace Platform.Aggregator.Grpc;

using System.Diagnostics;
using global::Grpc.Core;
using global::Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Клієнтський перехоплювач gRPC (Частина 6.1):
/// - Логування викликів, тривалості (Stopwatch) та кодів стану
/// - Прокидання заголовка x-correlation-id з HTTP контексту в метадані gRPC виклику
/// </summary>
public sealed class GrpcClientLoggingInterceptor : Interceptor
{
    private readonly ILogger<GrpcClientLoggingInterceptor> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GrpcClientLoggingInterceptor(
        ILogger<GrpcClientLoggingInterceptor> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString()
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString();

        var headers = context.Options.Headers ?? new Metadata();
        if (headers.GetValue("x-correlation-id") == null)
        {
            headers.Add("x-correlation-id", correlationId);
        }

        var newOptions = context.Options.WithHeaders(headers);
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method,
            context.Host,
            newOptions);

        _logger.LogInformation("gRPC client call starting: {Method} (CorrelationId: {CorrelationId})",
            context.Method.FullName, correlationId);

        var sw = Stopwatch.StartNew();
        var call = continuation(request, newContext);

        var responseAsync = HandleResponseAsync(call.ResponseAsync, context.Method.FullName, sw);

        return new AsyncUnaryCall<TResponse>(
            responseAsync,
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> HandleResponseAsync<TResponse>(
        Task<TResponse> task,
        string methodName,
        Stopwatch sw)
    {
        try
        {
            var response = await task;
            sw.Stop();
            _logger.LogInformation("gRPC client call finished: {Method} in {ElapsedMs} ms",
                methodName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (RpcException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "gRPC client call failed: {Method} with status {StatusCode} in {ElapsedMs} ms: {Detail}",
                methodName, ex.StatusCode, sw.ElapsedMilliseconds, ex.Status.Detail);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "gRPC client call failed: {Method} in {ElapsedMs} ms: {Message}",
                methodName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
