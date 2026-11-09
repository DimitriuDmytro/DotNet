namespace Platform.ServiceDefaults;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Делегуючий обробник для додавання заголовка X-Correlation-Id у вихідні HTTP-запити через HttpClient.
/// </summary>
public class CorrelationIdHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var correlationId = httpContext.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(correlationId) && !request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
            {
                request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
