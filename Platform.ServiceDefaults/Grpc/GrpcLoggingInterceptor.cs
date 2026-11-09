namespace Platform.ServiceDefaults.Grpc;

using System.Diagnostics;
using global::Grpc.Core;
using global::Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

/// <summary>
/// Серверний gRPC Interceptor для наскрізного логування викликів, статусів виконання,
/// тривалості та кореляційного ID (x-correlation-id) з метаданих запиту.
/// </summary>
public sealed class GrpcLoggingInterceptor : Interceptor
{
    private readonly ILogger<GrpcLoggingInterceptor> _logger;

    public GrpcLoggingInterceptor(ILogger<GrpcLoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = context.RequestHeaders.GetValue("x-correlation-id") ?? "unknown";

        _logger.LogDebug("gRPC incoming call {GrpcMethod} from {Peer} (CorrelationId {CorrelationId})",
            context.Method, context.Peer, correlationId);

        try
        {
            var response = await continuation(request, context);
            sw.Stop();

            _logger.LogInformation("gRPC {GrpcMethod} from {Peer}: {GrpcStatus} in {ElapsedMs} ms (CorrelationId {CorrelationId})",
                context.Method, context.Peer, StatusCode.OK, sw.ElapsedMilliseconds, correlationId);

            return response;
        }
        catch (RpcException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "gRPC {GrpcMethod} from {Peer}: {GrpcStatus} in {ElapsedMs} ms",
                context.Method, context.Peer, ex.StatusCode, sw.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "gRPC {GrpcMethod} failed with unhandled exception in {ElapsedMs} ms",
                context.Method, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
