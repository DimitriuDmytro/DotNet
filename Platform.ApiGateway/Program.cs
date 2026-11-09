using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Platform.ServiceDefaults;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

// Aspire Service Defaults
builder.AddServiceDefaults();

// YARP Reverse Proxy з підтримкою Aspire Service Discovery
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

var routingLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Gateway.Routing");

// CorrelationId Middleware ОБОВ'ЯЗКОВО до MapReverseProxy
app.UseMiddleware<CorrelationIdMiddleware>();

// Логування маршрутизації, IP клієнта, розміру відповіді та помилок зв'язку (GW-15, GW-16, GW-17)
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    await next();

    sw.Stop();

    var proxyFeature = context.GetReverseProxyFeature();
    var routeId = proxyFeature?.Route?.Config?.RouteId ?? "unmatched";
    var clusterId = proxyFeature?.Cluster?.Config?.ClusterId ?? "none";
    var destination = proxyFeature?.ProxiedDestination?.Model?.Config?.Address ?? "none";
    var statusCode = context.Response.StatusCode;
    var responseSize = context.Response.ContentLength;

    routingLogger.LogInformation(
        "Gateway: {Method} {Path} from {ClientIp} -> route '{RouteId}' (cluster '{ClusterId}', destination '{Destination}'): {StatusCode} [size: {ResponseSize} B] in {ElapsedMs} ms",
        context.Request.Method,
        context.Request.Path,
        clientIp,
        routeId,
        clusterId,
        destination,
        statusCode,
        responseSize ?? 0,
        sw.ElapsedMilliseconds);

    // Логування помилок недоступності сервісів / forwarder error (GW-16)
    var forwarderError = context.Features.Get<IForwarderErrorFeature>();
    if (forwarderError != null)
    {
        routingLogger.LogWarning(
            forwarderError.Exception,
            "Gateway Forwarder Warning: помилка перенаправлення запиту до кластера '{ClusterId}' (Error: {Error})",
            clusterId,
            forwarderError.Error);
    }
});

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
