using Concertable.DataAccess.Application;

namespace Concertable.B2B.Admin.Application.Interfaces;

internal interface IAdminRepository : IRepository<AdminInvitationEntity, Guid>
{
    /// <summary>Admins currently provisioned — the last-admin invariant reads this before a revoke.</summary>
    Task<int> CountAdminsAsync(CancellationToken ct = default);

    /// <summary>Every admin's sub — the admin list joins these to email via <c>IUserModule.GetEmailsByIdsAsync</c>.</summary>
    Task<IReadOnlyList<Guid>> ListAdminSubsAsync(CancellationToken ct = default);

    Task<bool> IsAdminAsync(Guid sub, CancellationToken ct = default);

    void GrantAdmin(Guid sub);

    void RemoveAdmin(Guid sub);

    /// <summary>Live (pending and unexpired at <paramref name="now"/>) admin invitations — the provisioning
    /// list. Lapsed rows stay <c>Pending</c> in storage, so the expiry cut-off is applied here.</summary>
    Task<IReadOnlyList<AdminInvitationEntity>> ListPendingInvitationsAsync(DateTime now, CancellationToken ct = default);

    /// <summary>The tracked pending invitation for <paramref name="email"/> (at most one — the filtered-unique
    /// index) or null. The caller checks its expiry: a live one blocks a duplicate invite, a lapsed one is
    /// retired before re-inviting.</summary>
    Task<AdminInvitationEntity?> GetPendingInvitationByEmailAsync(string email, CancellationToken ct = default);
}
