using System.Net.Http.Headers;

namespace Platform.Aggregator.Security;

public class IdentityServerAuthHandler : DelegatingHandler
{
    private readonly IIdentityServerTokenService _tokenService;

    public IdentityServerAuthHandler(IIdentityServerTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenService.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token) && request.Headers.Authorization == null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
