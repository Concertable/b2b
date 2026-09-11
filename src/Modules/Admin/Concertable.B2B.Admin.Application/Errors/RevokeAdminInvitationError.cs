using Concertable.B2B.Admin.Domain.Errors;
using Dunet;

namespace Concertable.B2B.Admin.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RevokeAdminInvitationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        InvitationNotFound(var invitationId) =>
            ErrorDefinition.NotFound<InvitationNotFound>($"Invitation {invitationId} was not found."),
        RevocationFailed(var error) => error.Definition
    };

    [ErrorCode("admin.revoke_invitation_not_found")]
    public partial record InvitationNotFound(Guid InvitationId);

    public partial record RevocationFailed(AdminInvitationRevocationError Error);
}
