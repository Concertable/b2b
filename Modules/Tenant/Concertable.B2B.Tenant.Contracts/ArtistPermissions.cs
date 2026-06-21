using System.Collections.Frozen;

namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// The artist persona's catalog: the <see cref="SharedPermissions"/> base plus the permissions only an artist
/// tenant can hold (design §1.3, marked (A)). <see cref="ApplicationsSubmit"/> lives here, not in a
/// persona-blind bag, so a venue tenant can never be granted it — its catalog doesn't contain it.
/// </summary>
public sealed class ArtistPermissions : IPermissionSet
{
    public const string ApplicationsSubmit = "applications.submit";

    private static readonly FrozenDictionary<TenantRole, FrozenSet<string>> Exclusive =
        new Dictionary<TenantRole, FrozenSet<string>>
        {
            [TenantRole.Owner] = new[] { ApplicationsSubmit }.ToFrozenSet(),
            [TenantRole.Manager] = new[] { ApplicationsSubmit }.ToFrozenSet(),
        }.ToFrozenDictionary();

    private readonly SharedPermissions shared;

    public ArtistPermissions(SharedPermissions shared) => this.shared = shared;

    public bool Grants(TenantRole role, string permission) =>
        shared.Grants(role, permission) ||
        (Exclusive.TryGetValue(role, out var permissions) && permissions.Contains(permission));

    /// <summary>The artist-exclusive permissions added on top of the shared base — checked against the declared constants by the catalog-coverage test.</summary>
    public static IReadOnlySet<string> All { get; } = Exclusive.Values.SelectMany(p => p).ToFrozenSet();
}
