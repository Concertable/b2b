using System.Collections.Frozen;

namespace Concertable.B2B.Tenant.Contracts;

/// <summary>Resolves permission grants through the catalog for the specified tenant type.</summary>
public sealed class PermissionCatalog : IPermissionCatalog
{
    private readonly FrozenDictionary<TenantType, IPermissionSet> byTenantType;

    public PermissionCatalog(VenuePermissions venue, ArtistPermissions artist) =>
        byTenantType = new Dictionary<TenantType, IPermissionSet>
        {
            [TenantType.Venue] = venue,
            [TenantType.Artist] = artist,
        }.ToFrozenDictionary();

    public bool Grants(TenantType tenantType, TenantRole role, string permission) =>
        byTenantType[tenantType].Grants(role, permission);
}
