namespace Concertable.B2B.Authorization.Contracts;

public sealed record MembershipSnapshot(
    Guid MembershipId,
    Guid TenantId,
    Guid UserId,
    long PermissionVersion,
    long RolePolicyVersion,
    PermissionSet Permissions)
{
    public bool HasPermission(TenantPermission permission) => Permissions[permission] != ResourceAccess.None;
}
