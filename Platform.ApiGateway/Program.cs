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

// CorrelationId Middleware ОБОВ'ЯЗКОВО до MapReverseProxy
app.UseMiddleware<CorrelationIdMiddleware>();

// Логування маршрутизації, IP клієнта, розміру відповіді та помилок зв'язку
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    await next();

    sw.Stop();

    var proxyFeature = context.GetReverseProxyFeature();
    var routeId = proxyFeature?.Route?.Config?.RouteId ?? "unmatched";
    var clusterId = proxyFeature?.Cluster?.Config?.ClusterId ?? "none";
    var statusCode = context.Response.StatusCode;
    var contentLength = context.Response.ContentLength ?? 0;

    app.Logger.LogInformation(
        "Gateway: {Method} {Path} from {ClientIp} -> route '{Route}' (cluster '{Cluster}'): {StatusCode} [size: {Bytes} B] in {Elapsed} ms",
        context.Request.Method,
        context.Request.Path,
        clientIp,
        routeId,
        clusterId,
        statusCode,
        contentLength,
        sw.ElapsedMilliseconds);

    // Логування помилок недоступності сервісів / forwarder error
    var forwarderError = context.Features.Get<IForwarderErrorFeature>();
    if (forwarderError != null)
    {
        app.Logger.LogWarning(
            forwarderError.Exception,
            "Gateway Forwarder Warning: помилка перенаправлення запиту до кластера '{Cluster}' (Error: {Error})",
            clusterId,
            forwarderError.Error);
    }
});

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
