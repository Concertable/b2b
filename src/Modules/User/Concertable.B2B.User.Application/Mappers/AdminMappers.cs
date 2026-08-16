using Concertable.B2B.User.Application.DTOs;
using Concertable.B2B.User.Application.Errors;
using Concertable.B2B.User.Domain.Errors;

namespace Concertable.B2B.User.Application.Mappers;

internal static class AdminMappers
{
    public static AdminInvitationDto ToDto(this AdminInvitationEntity invitation) =>
        new(invitation.Id, invitation.Email, invitation.CreatedAt, invitation.ExpiresAt);

    public static RevokeAdminInvitationError ToRevokeAdminInvitationError(
        this AdminInvitationRevocationError error) => error switch
        {
            AdminInvitationRevocationError.NotPending => new RevokeAdminInvitationError.InvitationNotPending()
        };
}
