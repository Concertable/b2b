namespace Concertable.B2B.Tenant.Application.Interfaces;

/// <summary>The Tenant module's GDPR-erasure operations for a subject: severing their memberships (and reporting
/// which tenants that leaves member-less, for sole-trader wind-down handling) and purging pending invitations
/// addressed to them. Runs admin-operated with no ambient tenant — every query is by explicit id.</summary>
internal interface ITenantErasureService
{
    /// <summary>Removes every membership row of the subject across all tenants; returns the tenants left with no
    /// members (wound down). Erasure supersedes the last-owner invariant — a data subject's right to erasure is
    /// not blocked by an internal ownership rule — so a sole-Owner's membership is severed and its tenant reported
    /// as wound down for the caller to handle.</summary>
    Task<IReadOnlyList<Guid>> SeverMembershipsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Purges pending invitations addressed to the subject's email (their PII). Accepted invitations are
    /// already severed with the membership.</summary>
    Task PurgePendingInvitationsAsync(string email, CancellationToken ct = default);
}
