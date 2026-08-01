using Concertable.B2B.Tenant.Application.Requests;

namespace Concertable.B2B.Tenant.Application.Interfaces;

/// <summary>
/// Invitation lifecycle for the caller's active tenant (create/list/revoke, resolved from <c>ITenantContext</c>
/// like <c>MembershipService</c>) plus the top-level accept. <see cref="AcceptInvitationAsync"/> takes no active
/// tenant — the accepting caller may belong to no tenant yet — and is gated on the caller's own email matching
/// the invitation.
/// </summary>
internal interface IInvitationService
{
    Task<IReadOnlyList<InvitationDto>> ListPendingInvitationsAsync(CancellationToken ct = default);
    Task<Result<InvitationDto, InviteMemberError>> InviteAsync(InviteMemberRequest request, CancellationToken ct = default);
    Task<UnitResult<RevokeInvitationError>> RevokeInvitationAsync(Guid invitationId, CancellationToken ct = default);
    Task<Result<MembershipDto, AcceptInvitationError>> AcceptInvitationAsync(Guid invitationId, CancellationToken ct = default);
}
