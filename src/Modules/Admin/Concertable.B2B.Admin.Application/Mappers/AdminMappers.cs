using Concertable.B2B.Admin.Application.DTOs;

namespace Concertable.B2B.Admin.Application.Mappers;

internal static class AdminMappers
{
    extension(AdminInvitationEntity invitation)
    {
        public AdminInvitationDto ToDto() =>
            new(invitation.Id, invitation.Email, invitation.CreatedAt, invitation.ExpiresAt);
    }
}
