using Concertable.B2B.User.Application.Errors;
using Concertable.B2B.User.Domain.Errors;

namespace Concertable.B2B.User.Application.Mappers;

internal static class AdminErrorMappers
{
    extension(AdminInvitationRevocationError error)
    {
        public RevokeAdminInvitationError ToRevokeAdminInvitationError() => error switch
        {
            AdminInvitationRevocationError.NotPending => new RevokeAdminInvitationError.InvitationNotPending()
        };
    }
}
