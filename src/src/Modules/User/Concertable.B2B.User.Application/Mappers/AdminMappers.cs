using Concertable.B2B.User.Application.DTOs;

namespace Concertable.B2B.User.Application.Mappers;

internal static class AdminMappers
{
    extension(AdminInvitationEntity invitation)
    {
        public AdminInvitationDto ToDto() =>
            new(invitation.Id, invitation.Email, invitation.CreatedAt, invitation.ExpiresAt);
    }
}
