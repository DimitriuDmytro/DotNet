namespace Platform.Security.Permissions;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Вимога наявності конкретного дозволу (Permission) для виконання операції (Критерій 13).
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission cannot be empty.", nameof(permission));
        }

        Permission = permission.Trim();
    }
}
