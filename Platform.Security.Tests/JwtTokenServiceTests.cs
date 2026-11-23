using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Platform.Security.Jwt;
using Services.Auth.Api.Data;
using Services.Auth.Api.Models;
using Services.Auth.Api.Services;
using Xunit;

namespace Platform.Security.Tests;

public class JwtTokenServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly IOptions<JwtSettings> _options;
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(dbOptions);

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _options = Options.Create(new JwtSettings
        {
            Issuer = "http://test-issuer",
            Audience = "test-audience",
            SecretKey = "SuperSecretKeyForTestingJwtTokenGenerationMustBeLongEnough123!",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });

        _service = new JwtTokenService(_db, _userManagerMock.Object, _options, NullLogger<JwtTokenService>.Instance);
    }

    [Fact]
    public void HashToken_ShouldProduceConsistentSha256Hex()
    {
        // Arrange
        var token = "SampleRefreshTokenForTestingPurposes12345";

        // Act
        var hash1 = JwtTokenService.HashToken(token);
        var hash2 = JwtTokenService.HashToken(token);

        // Assert
        hash1.Should().NotBeNullOrEmpty();
        hash1.Should().Be(hash2);
        hash1.Length.Should().Be(64); // 256 bits = 32 bytes = 64 hex characters
    }

    [Fact]
    public async Task GenerateTokensAsync_ShouldSaveHashedRefreshTokenInDatabase()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "usr-1",
            UserName = "testuser",
            Email = "test@example.com"
        };

        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var (accessToken, rawRefreshToken) = await _service.GenerateTokensAsync(user, "127.0.0.1");

        // Assert
        accessToken.Should().NotBeNullOrEmpty();
        rawRefreshToken.Should().NotBeNullOrEmpty();

        var hashed = JwtTokenService.HashToken(rawRefreshToken);
        var tokenInDb = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hashed);

        tokenInDb.Should().NotBeNull();
        tokenInDb!.UserId.Should().Be("usr-1");
        tokenInDb.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_WhenValid_ShouldRevokeOldAndCreateNew()
    {
        // Arrange
        var user = new ApplicationUser { Id = "usr-1", UserName = "testuser", Email = "test@example.com" };
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
        _userManagerMock.Setup(m => m.FindByIdAsync("usr-1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);

        var (_, rawRefreshToken) = await _service.GenerateTokensAsync(user, "127.0.0.1");

        // Act
        var (newAccessToken, newRawRefreshToken) = await _service.RotateRefreshTokenAsync(rawRefreshToken, "127.0.0.1");

        // Assert
        newAccessToken.Should().NotBeNullOrEmpty();
        newRawRefreshToken.Should().NotBeNullOrEmpty();
        newRawRefreshToken.Should().NotBe(rawRefreshToken);

        var oldHash = JwtTokenService.HashToken(rawRefreshToken);
        var oldToken = await _db.RefreshTokens.FirstAsync(t => t.TokenHash == oldHash);
        oldToken.IsRevoked.Should().BeTrue();
        oldToken.ReplacedByTokenHash.Should().Be(JwtTokenService.HashToken(newRawRefreshToken));
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_WhenRevokedTokenReused_ShouldRevokeAllUserTokensAndThrow()
    {
        // Arrange
        var user = new ApplicationUser { Id = "usr-victim", UserName = "victim", Email = "victim@example.com" };
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
        _userManagerMock.Setup(m => m.FindByIdAsync("usr-victim")).ReturnsAsync(user);

        // Generate token 1
        var (_, token1) = await _service.GenerateTokensAsync(user, "127.0.0.1");

        // Legitimate rotation: rotates token1 -> token2
        var (_, token2) = await _service.RotateRefreshTokenAsync(token1, "127.0.0.1");

        // Act: attacker attempts to reuse old token1
        var act = () => _service.RotateRefreshTokenAsync(token1, "192.168.1.100");

        // Assert
        await act.Should().ThrowAsync<SecurityTokenException>()
            .WithMessage("*reuse detected*");

        // Verify that ALL active tokens for user (including token2) were revoked!
        var activeTokens = await _db.RefreshTokens.Where(t => t.UserId == "usr-victim" && t.RevokedAt == null).ToListAsync();
        activeTokens.Should().BeEmpty();
    }
}
