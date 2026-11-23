using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Security.Permissions;
using Xunit;

namespace Platform.Security.Tests;

public class PermissionAuthorizationHandlerTests
{
    private readonly PermissionAuthorizationHandler _handler;

    public PermissionAuthorizationHandlerTests()
    {
        _handler = new PermissionAuthorizationHandler(NullLogger<PermissionAuthorizationHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsAdmin_ShouldSucceedWithoutSpecificPermission()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));

        var requirement = new PermissionRequirement("orders:delete");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasPermissionsClaim_ShouldSucceed()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("permissions", "orders:create"),
            new Claim("permissions", "orders:read")
        }, "TestAuth"));

        var requirement = new PermissionRequirement("orders:create");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasScopeClaimWithDotNotation_ShouldSucceed()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("scope", "products.read")
        }, "TestAuth"));

        var requirement = new PermissionRequirement("products:read");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasKeycloakUmaAuthorizationClaim_ShouldSucceed()
    {
        // Arrange
        var umaJson = """
        {
            "permissions": [
                {
                    "rsname": "orders-api",
                    "scopes": ["orders:read", "orders:create"]
                }
            ]
        }
        """;

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("authorization", umaJson)
        }, "TestAuth"));

        var requirement = new PermissionRequirement("orders:read");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenUserLacksRequiredPermission_ShouldNotSucceed()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("permissions", "orders:read")
        }, "TestAuth"));

        var requirement = new PermissionRequirement("orders:delete");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenClaimsAreMalformed_ShouldFailSafelyWithoutThrowing()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("authorization", "{ malformed json string !!!")
        }, "TestAuth"));

        var requirement = new PermissionRequirement("orders:read");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        var act = () => _handler.HandleAsync(context);

        // Assert
        await act.Should().NotThrowAsync();
        context.HasSucceeded.Should().BeFalse();
    }
}
