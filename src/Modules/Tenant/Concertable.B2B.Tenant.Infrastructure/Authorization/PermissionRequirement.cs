using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

/// <summary>A required <see cref="Permissions"/> constant, optionally scoped to a tenant persona.</summary>
internal sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission, TenantType? persona)
    {
        Permission = permission;
        Persona = persona;
    }

    public string Permission { get; }
    public TenantType? Persona { get; }
}
