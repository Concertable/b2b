using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// Gates an endpoint on a <see cref="Permissions"/> constant — the modern string-permission shape where the
/// permission <em>is</em> the policy name (resolved on demand by <c>PermissionPolicyProvider</c>). The
/// optional <see cref="TenantType"/> pins the active tenant's persona, which is what replaces the old
/// <c>[VenueManager]</c>/<c>[ArtistManager]</c> split: a venue-only endpoint is
/// <c>[HasPermission(Permissions.X, TenantType.Venue)]</c>.
/// </summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        => Policy = PermissionPolicy.Name(permission);

    public HasPermissionAttribute(string permission, TenantType persona)
        => Policy = PermissionPolicy.Name(permission, persona);
}
