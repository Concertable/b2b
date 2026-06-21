namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// Resolves a permission check against the active tenant's persona catalog — the single seam call-sites and
/// the authorization handler use. Persona is the active tenant's <see cref="TenantType"/> (resolved per
/// request, immutable for the tenant's life), never a call-site argument, so a venue tenant is checked
/// against <see cref="VenuePermissions"/> and an artist against <see cref="ArtistPermissions"/> by
/// construction — there is no way to grant one persona the other's exclusive permission.
/// </summary>
public interface IPermissionCatalog
{
    /// <summary>True iff <paramref name="role"/> in a <paramref name="persona"/> tenant is granted <paramref name="permission"/>.</summary>
    bool Grants(TenantType persona, TenantRole role, string permission);
}
