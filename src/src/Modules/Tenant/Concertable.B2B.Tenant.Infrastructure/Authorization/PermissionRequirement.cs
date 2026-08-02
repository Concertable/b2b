using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

/// <summary>A permission required by an authorization policy.</summary>
internal sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;

    public string Permission { get; }
}
