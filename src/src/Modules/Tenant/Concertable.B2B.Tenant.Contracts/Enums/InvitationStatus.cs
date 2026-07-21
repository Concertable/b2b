namespace Concertable.B2B.Tenant.Contracts.Enums;

/// <summary>
/// The lifecycle state of a <c>TenantInvitation</c>. <see cref="Pending"/> is the only state that can be
/// accepted or revoked; the rest are terminal. Exactly one live (<see cref="Pending"/>) invitation is
/// allowed per <c>(TenantId, Email)</c>.
/// </summary>
public enum InvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Revoked = 3,
    Expired = 4,
}
