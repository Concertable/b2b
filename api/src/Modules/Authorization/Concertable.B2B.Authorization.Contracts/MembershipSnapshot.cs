namespace Concertable.B2B.Authorization.Contracts;

/// <summary>
/// One membership incarnation as authorization needs it. <see cref="MembershipId"/> is part of the identity:
/// a removed and rejoined member is a different membership, so a request resolved against the old one denies
/// even when its role and version happen to match.
/// </summary>
public sealed record MembershipSnapshot(
    Guid MembershipId,
    Guid TenantId,
    Guid UserId,
    TenantRole Role,
    long PermissionVersion);
