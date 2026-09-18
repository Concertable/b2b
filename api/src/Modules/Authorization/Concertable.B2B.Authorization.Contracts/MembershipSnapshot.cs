namespace Concertable.B2B.Authorization.Contracts;

/// <summary>One membership incarnation. A rejoined member is a different MembershipId, so a request resolved
/// against the old one denies even when role and version match.</summary>
public sealed record MembershipSnapshot(
    Guid MembershipId,
    Guid TenantId,
    Guid UserId,
    TenantRole Role,
    long PermissionVersion);
