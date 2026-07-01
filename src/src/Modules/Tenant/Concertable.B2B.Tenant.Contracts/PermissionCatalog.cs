using System.Collections.Frozen;

namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// The keyed strategy resolver (see <c>api/docs/CODE_PATTERNS.md</c>) over the persona catalogs: maps the
/// active tenant's <see cref="TenantType"/> to its <see cref="IPermissionSet"/> and delegates. Consumers
/// inject <see cref="IPermissionCatalog"/> and never branch on persona or touch the map. A code-defined map,
/// not a <c>RolePermission</c> table: unit-testable, versioned with code, no admin UI, no per-tenant custom
/// roles. Registered singleton; the strategies are injected as concrete types.
/// </summary>
public sealed class PermissionCatalog : IPermissionCatalog
{
    private readonly FrozenDictionary<TenantType, IPermissionSet> byPersona;

    public PermissionCatalog(VenuePermissions venue, ArtistPermissions artist) =>
        byPersona = new Dictionary<TenantType, IPermissionSet>
        {
            [TenantType.Venue] = venue,
            [TenantType.Artist] = artist,
        }.ToFrozenDictionary();

    public bool Grants(TenantType persona, TenantRole role, string permission) =>
        byPersona[persona].Grants(role, permission);
}
