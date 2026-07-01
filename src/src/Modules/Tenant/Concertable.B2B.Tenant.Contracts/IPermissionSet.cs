namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// A queryable set of role→permission grants (design §1.3) — the one capability shared by the both-persona
/// base (<see cref="SharedPermissions"/>) and each persona's catalog (<see cref="VenuePermissions"/>,
/// <see cref="ArtistPermissions"/>), which compose the base with their exclusive grants. "Persona" is not a
/// property of the set; it is what <see cref="IPermissionCatalog"/> keys on to pick the venue or artist set.
/// </summary>
public interface IPermissionSet
{
    /// <summary>True iff <paramref name="role"/>'s bundle in this set contains <paramref name="permission"/>.</summary>
    bool Grants(TenantRole role, string permission);
}
