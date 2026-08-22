using Concertable.DataAccess.Application;

namespace Concertable.B2B.Admin.Application.Interfaces;

internal interface IAdminInvitationRepository : IRepository<AdminInvitationEntity, Guid>
{
    /// <summary>Live (pending and unexpired at <paramref name="now"/>) admin invitations — the provisioning
    /// list. Lapsed rows stay <c>Pending</c> in storage, so the expiry cut-off is applied here.</summary>
    Task<IReadOnlyList<AdminInvitationEntity>> ListPendingInvitationsAsync(DateTime now, CancellationToken ct = default);

    /// <summary>The tracked pending invitation for <paramref name="email"/> (at most one — the filtered-unique
    /// index) or null. The caller checks its expiry: a live one blocks a duplicate invite, a lapsed one is
    /// retired before re-inviting.</summary>
    Task<AdminInvitationEntity?> GetPendingInvitationByEmailAsync(string email, CancellationToken ct = default);
}
