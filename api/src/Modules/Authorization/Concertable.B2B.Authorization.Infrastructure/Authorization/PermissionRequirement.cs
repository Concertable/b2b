using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(TenantPermission permission)
    {
        this.Permission = permission;
    }

    public TenantPermission Permission { get; }
}
