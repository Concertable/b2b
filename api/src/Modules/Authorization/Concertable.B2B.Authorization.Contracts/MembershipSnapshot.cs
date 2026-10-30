using System.Collections.Immutable;

namespace Concertable.B2B.Authorization.Contracts;

public sealed record MembershipSnapshot(
    Guid MembershipId,
    Guid TenantId,
    Guid UserId,
    long PermissionVersion,
    long RolePolicyVersion,
    ImmutableDictionary<TenantPermission, ResourceCoverage> Permissions)
{
    public ResourceCoverage CoverageFor(TenantPermission permission) =>
        permission != default && Permissions.TryGetValue(permission, out var coverage)
            ? coverage
            : ResourceCoverage.None;

    public bool HasPermission(TenantPermission permission) => CoverageFor(permission) != ResourceCoverage.None;

    public bool HasSameAuthorityAs(MembershipSnapshot? other) =>
        other is not null
        && MembershipId == other.MembershipId
        && TenantId == other.TenantId
        && UserId == other.UserId
        && PermissionVersion == other.PermissionVersion
        && RolePolicyVersion == other.RolePolicyVersion
        && Permissions.Count == other.Permissions.Count
        && Permissions.All(pair => other.Permissions.TryGetValue(pair.Key, out var coverage)
            && coverage == pair.Value);
}
