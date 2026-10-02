using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Authorization.Contracts;

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permissionName)
    {
        if (!TenantPermission.TryParse(permissionName, out var permission))
            throw new ArgumentException($"Unknown tenant permission '{permissionName}'.", nameof(permissionName));

        Policy = PermissionPolicy.Name(permission);
    }
}
