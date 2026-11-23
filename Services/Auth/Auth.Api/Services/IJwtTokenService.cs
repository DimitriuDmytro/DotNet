using Microsoft.AspNetCore.Http;
using Services.Auth.Api.DTOs;
using Services.Auth.Api.Models;

namespace Services.Auth.Api.Services;

public interface IJwtTokenService
{
    Task<(string AccessToken, string RawRefreshToken)> GenerateTokensAsync(
        ApplicationUser user,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<(string AccessToken, string RawRefreshToken)> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task RevokeAllUserTokensAsync(
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    void SetRefreshTokenCookie(HttpResponse response, string refreshToken);
    void DeleteRefreshTokenCookie(HttpResponse response);
}
