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

    /// <summary>Grants admin for <paramref name="sub"/> if a matching pending invitation exists, or if
    /// <paramref name="email"/> is the bootstrap email and no admin exists yet, then saves. Called from
    /// the User module's registration handler inside its cross-module unit of work — this method's own
    /// save enlists in that ambient transaction rather than committing independently, so user creation
    /// and admin granting land atomically. A redelivered call is naturally a no-op: the invitation is no
    /// longer Pending, or an admin already exists.</summary>
    Task GrantIfEligibleAsync(Guid sub, string email, CancellationToken ct = default);
}
