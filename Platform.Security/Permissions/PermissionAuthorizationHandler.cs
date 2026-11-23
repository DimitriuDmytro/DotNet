namespace Platform.Security.Permissions;

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Обробник перевірки прав доступу PermissionAuthorizationHandler (Критерій 13).
/// Зчитує permissions з токена Keycloak (claim 'authorization'), IdentityServer scopes або ролей.
/// Безпечно обробляє відсутність або некоректність claim без винятків.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var user = context.User;
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            return Task.CompletedTask;
        }

        // 1. Перевірка ролі Admin (відповідно до матриці: admin має повний доступ ALL до всіх ресурсів)
        if (user.IsInRole("Admin") || user.IsInRole("admin") ||
            user.HasClaim(c => (c.Type is "role" or ClaimTypes.Role or "roles") &&
                               c.Value.Equals("admin", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug("User has Admin role. Permission {Permission} automatically granted.", requirement.Permission);
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. Перевірка claim 'authorization' (формат Keycloak UMA RPT: {"permissions":[{"rsname":"...","scopes":["orders:read"]}]})
        var authClaim = user.FindFirst("authorization")?.Value;
        if (!string.IsNullOrWhiteSpace(authClaim))
        {
            if (CheckKeycloakAuthorizationClaim(authClaim, requirement.Permission))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        // 3. Перевірка прямих claims 'permission' або 'permissions'
        var permissionClaims = user.FindAll(c => c.Type is "permission" or "permissions");
        foreach (var claim in permissionClaims)
        {
            if (string.Equals(claim.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Якщо значення є JSON-масивом
            if (claim.Value.TrimStart().StartsWith("["))
            {
                try
                {
                    using var doc = JsonDocument.Parse(claim.Value);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            if (string.Equals(elem.GetString(), requirement.Permission, StringComparison.OrdinalIgnoreCase))
                            {
                                context.Succeed(requirement);
                                return Task.CompletedTask;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse JSON permissions claim: {Value}", claim.Value);
                }
            }
        }

        // 4. Перевірка IdentityServer / OAuth 2.0 scopes ('scope')
        // Наприклад, 'orders:read' або 'orders.read'
        var normalizedRequiredScope = requirement.Permission.Replace(':', '.');
        var scopeClaims = user.FindAll("scope");
        foreach (var claim in scopeClaims)
        {
            var scopes = claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var scope in scopes)
            {
                if (string.Equals(scope, requirement.Permission, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(scope, normalizedRequiredScope, StringComparison.OrdinalIgnoreCase))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }

        _logger.LogDebug("Permission {Permission} denied for user {User}", requirement.Permission, user.Identity.Name);
        return Task.CompletedTask;
    }

    private bool CheckKeycloakAuthorizationClaim(string json, string requiredPermission)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("permissions", out var permissionsElement))
            {
                return false;
            }

            if (permissionsElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var perm in permissionsElement.EnumerateArray())
            {
                if (!perm.TryGetProperty("scopes", out var scopesElement) || scopesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var scope in scopesElement.EnumerateArray())
                {
                    var scopeStr = scope.GetString();
                    if (string.Equals(scopeStr, requiredPermission, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Malformed 'authorization' claim JSON encountered");
        }

        return false;
    }
}
