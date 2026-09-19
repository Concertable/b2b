namespace Concertable.B2B.Authorization.Contracts;

public sealed record MembershipSnapshot(
    Guid MembershipId,
    Guid TenantId,
    Guid UserId,
    TenantRole Role,
    long PermissionVersion);
