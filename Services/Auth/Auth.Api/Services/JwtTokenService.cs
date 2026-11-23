using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Platform.Security.Jwt;
using Services.Auth.Api.Data;
using Services.Auth.Api.Models;

namespace Services.Auth.Api.Services;

public class JwtTokenService : IJwtTokenService
{
    private const string RefreshTokenCookieName = "refreshToken";
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings,
        ILogger<JwtTokenService> logger)
    {
        _db = db;
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<(string AccessToken, string RawRefreshToken)> GenerateTokensAsync(
        ApplicationUser user,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GenerateAccessTokenAsync(user);
        var rawRefreshToken = GenerateSecureRandomToken();
        var tokenHash = HashToken(rawRefreshToken);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays <= 0 ? 7 : _jwtSettings.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated access and refresh tokens for user {UserId}", user.Id);
        return (accessToken, rawRefreshToken);
    }

    public async Task<(string AccessToken, string RawRefreshToken)> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var existingToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null)
        {
            _logger.LogWarning("Refresh token rotation attempted with non-existent token");
            throw new SecurityTokenException("Invalid refresh token");
        }

        if (existingToken.IsRevoked)
        {
            _logger.LogWarning("Refresh token reuse detected for user {UserId}! Revoking all active tokens.", existingToken.UserId);
            await RevokeAllUserTokensAsync(existingToken.UserId, ipAddress, cancellationToken);
            throw new SecurityTokenException("Refresh token reuse detected. All sessions revoked.");
        }

        if (existingToken.IsExpired)
        {
            _logger.LogWarning("Refresh token is expired for user {UserId}", existingToken.UserId);
            throw new SecurityTokenException("Refresh token is expired");
        }

        var user = await _userManager.FindByIdAsync(existingToken.UserId);
        if (user == null || await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("User {UserId} not found or locked out during token rotation", existingToken.UserId);
            throw new SecurityTokenException("User is inactive or locked out");
        }

        var newRawRefreshToken = GenerateSecureRandomToken();
        var newTokenHash = HashToken(newRawRefreshToken);

        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.RevokedByIp = ipAddress;
        existingToken.ReplacedByTokenHash = newTokenHash;

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays <= 0 ? 7 : _jwtSettings.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        var newAccessToken = await GenerateAccessTokenAsync(user);
        _logger.LogInformation("Rotated refresh token for user {UserId}", user.Id);

        return (newAccessToken, newRawRefreshToken);
    }

    public async Task<bool> RevokeRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token == null || token.IsRevoked)
        {
            return false;
        }

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Revoked refresh token for user {UserId}", token.UserId);
        return true;
    }

    public async Task RevokeAllUserTokensAsync(
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Revoked {Count} active tokens for user {UserId}", activeTokens.Count, userId);
    }

    public void SetRefreshTokenCookie(HttpResponse response, string refreshToken)
    {
        var days = _jwtSettings.RefreshTokenDays <= 0 ? 7 : _jwtSettings.RefreshTokenDays;
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(days),
            Path = "/"
        };

        response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    public void DeleteRefreshTokenCookie(HttpResponse response)
    {
        response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
    }

    private async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        var minutes = _jwtSettings.AccessTokenMinutes <= 0 ? 15 : _jwtSettings.AccessTokenMinutes;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(minutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = creds
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string GenerateSecureRandomToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}
