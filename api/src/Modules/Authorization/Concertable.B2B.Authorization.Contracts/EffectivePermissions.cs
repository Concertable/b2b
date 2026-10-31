using System.Collections;
using System.Collections.Immutable;

namespace Concertable.B2B.Authorization.Contracts;

public readonly struct EffectivePermissions
    : IEquatable<EffectivePermissions>, IEnumerable<KeyValuePair<TenantPermission, ResourceAccess>>
{
    private readonly ImmutableDictionary<TenantPermission, ResourceAccess>? entries;

    private EffectivePermissions(ImmutableDictionary<TenantPermission, ResourceAccess> entries) =>
        this.entries = entries;

    public static EffectivePermissions From(IEnumerable<KeyValuePair<TenantPermission, ResourceAccess>> grants) =>
        new(grants
            .GroupBy(grant => grant.Key)
            .ToImmutableDictionary(group => group.Key, group => group.Max(grant => grant.Value)));

    public ResourceAccess this[TenantPermission permission] =>
        permission != default && entries is not null && entries.TryGetValue(permission, out var access)
            ? access
            : ResourceAccess.None;

    private ImmutableDictionary<TenantPermission, ResourceAccess> Entries =>
        entries ?? ImmutableDictionary<TenantPermission, ResourceAccess>.Empty;

    public bool Equals(EffectivePermissions other)
    {
        var theirs = other.Entries;
        return Entries.Count == theirs.Count
            && Entries.All(entry => theirs.TryGetValue(entry.Key, out var access) && access == entry.Value);
    }

    public override bool Equals(object? obj) => obj is EffectivePermissions other && Equals(other);

    public override int GetHashCode() =>
        Entries.Aggregate(0, (hash, entry) => hash ^ HashCode.Combine(entry.Key, entry.Value));

    public static bool operator ==(EffectivePermissions left, EffectivePermissions right) => left.Equals(right);

    public static bool operator !=(EffectivePermissions left, EffectivePermissions right) => !left.Equals(right);

    public IEnumerator<KeyValuePair<TenantPermission, ResourceAccess>> GetEnumerator() =>
        ((IEnumerable<KeyValuePair<TenantPermission, ResourceAccess>>)Entries).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
