using Concertable.B2B.User.Application.DTOs;
using Concertable.B2B.User.Application.Errors;
using Concertable.B2B.User.Domain.Errors;

namespace Concertable.B2B.User.Application.Mappers;

internal static class AdminMappers
{
    extension(AdminInvitationEntity invitation)
    {
        public AdminInvitationDto ToDto() =>
            new(invitation.Id, invitation.Email, invitation.CreatedAt, invitation.ExpiresAt);
    }

    extension(AdminInvitationRevocationError error)
    {
        public RevokeAdminInvitationError ToRevokeAdminInvitationError() => error switch
        {
            AdminInvitationRevocationError.NotPending => new RevokeAdminInvitationError.InvitationNotPending()
        };
    }
}
