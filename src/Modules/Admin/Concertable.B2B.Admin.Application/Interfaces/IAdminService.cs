using Concertable.B2B.Admin.Application.DTOs;
using Concertable.B2B.Admin.Application.Errors;
using Concertable.B2B.Admin.Application.Requests;

namespace Concertable.B2B.Admin.Application.Interfaces;

internal interface IAdminService
{
    Task<AdminOverview> GetOverviewAsync(CancellationToken ct = default);
    Task<Result<AdminInvitationDto, InviteAdminError>> InviteAsync(CreateAdminInvitationRequest request, CancellationToken ct = default);
    Task<UnitResult<RevokeAdminInvitationError>> RevokeInvitationAsync(Guid invitationId, CancellationToken ct = default);
    Task<UnitResult<RevokeAdminError>> RevokeAdminAsync(Guid sub, CancellationToken ct = default);
    Task<bool> IsCurrentUserAdminAsync(CancellationToken ct = default);

    /// <summary>Grants the current user admin off a matching pending invitation or the one-time bootstrap
    /// email, if they aren't already an admin, then returns whether the caller is an admin afterward.
    /// Called from <c>UserController.Me()</c> — the first authenticated request after login, which
    /// Auth's own login gate (<c>CanAuthenticate</c> requires <c>IsEmailVerified</c>) guarantees runs
    /// only for a verified mailbox. No-op (returns the pre-existing status) otherwise.</summary>
    Task<bool> EnsureCurrentUserAdminGrantedIfEligibleAsync(CancellationToken ct = default);
}
