namespace Platform.Security.Permissions;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Декларативний атрибут перевірки прав [RequirePermission("orders:create")] (Критерій 13).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
        : base(policy: $"{PolicyPrefix}{permission}")
    {
        Permission = permission;
    }

    public RequirePermissionAttribute(string resource, string action)
        : this($"{resource}:{action}")
    {
    }
}
