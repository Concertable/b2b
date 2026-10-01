using System.Collections.Immutable;

namespace Concertable.B2B.Authorization.Contracts;

public sealed record MembershipSnapshot(
    Guid MembershipId,
    Guid TenantId,
    Guid UserId,
    long PermissionVersion,
    long RolePolicyVersion,
    ImmutableDictionary<TenantPermission, ResourceAudience> Permissions)
{
    public ResourceAudience AudienceFor(TenantPermission permission) =>
        permission != default && Permissions.TryGetValue(permission, out var audience)
            ? audience
            : ResourceAudience.None;

    public bool HasPermission(TenantPermission permission) => AudienceFor(permission) != ResourceAudience.None;

    public bool HasSameAuthorityAs(MembershipSnapshot? other) =>
        other is not null
        && MembershipId == other.MembershipId
        && TenantId == other.TenantId
        && UserId == other.UserId
        && PermissionVersion == other.PermissionVersion
        && RolePolicyVersion == other.RolePolicyVersion
        && Permissions.Count == other.Permissions.Count
        && Permissions.All(pair => other.Permissions.TryGetValue(pair.Key, out var audience)
            && audience == pair.Value);
}
