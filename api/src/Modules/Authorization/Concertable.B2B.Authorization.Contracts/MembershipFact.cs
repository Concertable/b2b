namespace Concertable.B2B.Authorization.Contracts;

/// <summary>
/// One membership as authorization needs it — identity, role and the revision a read re-checks against.
/// Carries no tenant profile, legal name or contact fact: those belong to the operations whose eligibility
/// policy needs them, not to request authority.
/// </summary>
public sealed record MembershipFact(Guid TenantId, Guid UserId, TenantRole Role, long AuthorizationVersion);
