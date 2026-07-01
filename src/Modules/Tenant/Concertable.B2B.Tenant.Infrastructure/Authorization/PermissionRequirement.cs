using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

/// <summary>A required permission constant. Persona is enforced by the catalog (permission-persona) and the
/// endpoint's <c>TenantPersonaAttribute</c> (surface-persona), not carried on the requirement.</summary>
internal sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;

    public string Permission { get; }
}
