using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface IInvitationRepository : IRepository<TenantInvitationEntity, Guid>
{
    /// <summary>Every invitation row of a tenant — the delete-org cascade removes them so no invitation outlives its tenant.</summary>
    Task<IReadOnlyList<TenantInvitationEntity>> ListInvitationsByTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Live (pending and unexpired at <paramref name="now"/>) invitations for a tenant — the
    /// members-management "pending invites" list. Lapsed rows stay <c>Pending</c> in storage, so the expiry
    /// cut-off is applied here rather than trusting <c>Status</c> alone.</summary>
    Task<IReadOnlyList<TenantInvitationEntity>> ListPendingInvitationsByTenantAsync(Guid tenantId, DateTime now, CancellationToken ct = default);

    /// <summary>The tracked pending invitation for <c>(tenant, email)</c> (at most one — the filtered-unique
    /// index) or null. The caller checks its expiry: a live one blocks a duplicate invite, a lapsed one is
    /// retired before re-inviting.</summary>
    Task<TenantInvitationEntity?> GetPendingInvitationByEmailAsync(Guid tenantId, string email, CancellationToken ct = default);

    /// <summary>Pending invitations addressed to an email across all tenants — GDPR erasure purges the invitee's
    /// PII (the accepted rows are already severed with the membership).</summary>
    Task<IReadOnlyList<TenantInvitationEntity>> ListPendingInvitationsByEmailAsync(string email, CancellationToken ct = default);
}
