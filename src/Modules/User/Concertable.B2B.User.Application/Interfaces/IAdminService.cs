using Concertable.B2B.User.Application.DTOs;
using Concertable.B2B.User.Application.Errors;
using Concertable.B2B.User.Application.Requests;

namespace Concertable.B2B.User.Application.Interfaces;

internal interface IAdminService
{
    Task<AdminOverviewDto> GetOverviewAsync(CancellationToken ct = default);
    Task<Result<AdminInvitationDto, InviteAdminError>> InviteAsync(CreateAdminInvitationRequest request, CancellationToken ct = default);
    Task<UnitResult<RevokeAdminInvitationError>> RevokeInvitationAsync(Guid invitationId, CancellationToken ct = default);
    Task<UnitResult<RevokeAdminError>> RevokeAdminAsync(Guid sub, CancellationToken ct = default);
    Task<bool> IsCurrentUserAdminAsync(CancellationToken ct = default);
}
