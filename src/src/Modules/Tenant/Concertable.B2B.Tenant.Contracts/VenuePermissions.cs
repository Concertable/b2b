using System.Collections.Frozen;

namespace Concertable.B2B.Tenant.Contracts;

public sealed class VenuePermissions : IPermissionSet
{
    public const string OpportunitiesManage = "opportunities.manage";
    public const string ApplicationsDecide = "applications.decide";
    public const string ConcertsManage = "concerts.manage";

    // Reserved — defined now so the six-role matrix is meaningful; first enforced when day-of-show ships.
    public const string ConcertsCheckIn = "concerts.check_in";

    private static readonly FrozenDictionary<TenantRole, FrozenSet<string>> Exclusive =
        new Dictionary<TenantRole, FrozenSet<string>>
        {
            [TenantRole.Owner] = new[]
            {
                OpportunitiesManage, ApplicationsDecide, ConcertsManage, ConcertsCheckIn,
            }.ToFrozenSet(),

            [TenantRole.Manager] = new[]
            {
                OpportunitiesManage, ApplicationsDecide, ConcertsManage, ConcertsCheckIn,
            }.ToFrozenSet(),

            [TenantRole.Staff] = new[] { ConcertsCheckIn }.ToFrozenSet(),

            [TenantRole.Door] = new[] { ConcertsCheckIn }.ToFrozenSet(),
        }.ToFrozenDictionary();

    private readonly SharedPermissions shared;

    public VenuePermissions(SharedPermissions shared) => this.shared = shared;

    public bool Grants(TenantRole role, string permission) =>
        shared.Grants(role, permission) ||
        (Exclusive.TryGetValue(role, out var permissions) && permissions.Contains(permission));

    /// <summary>The venue-exclusive permissions added on top of the shared base — checked against the declared constants by the catalog-coverage test.</summary>
    public static IReadOnlySet<string> All { get; } = Exclusive.Values.SelectMany(p => p).ToFrozenSet();
}
