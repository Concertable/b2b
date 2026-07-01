using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// Gates an endpoint on a permission constant — the modern string-permission shape where the permission
/// <em>is</em> the policy name (resolved on demand by <c>PermissionPolicyProvider</c>). Pass a constant from
/// the permission catalog: <see cref="SharedPermissions"/> for a both-persona permission,
/// <see cref="VenuePermissions"/>/<see cref="ArtistPermissions"/> for a persona-exclusive one — the latter
/// carry their persona by construction (a venue tenant's catalog has no artist permission, and vice versa).
/// A controller's surface persona, for shared permissions on a single-persona surface, comes from
/// <see cref="TenantPersonaAttribute"/>.
/// </summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        => Policy = PermissionPolicy.Name(permission);
}
