using Dunet;

namespace Concertable.B2B.User.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RevokeAdminInvitationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        InvitationNotFound(var invitationId) =>
            ErrorDefinition.NotFound<InvitationNotFound>($"Invitation {invitationId} was not found."),
        InvitationNotPending =>
            ErrorDefinition.Conflict<InvitationNotPending>("Only a pending invitation can be revoked.")
    };

    [ErrorCode("admin.revoke_invitation_not_found")]
    public partial record InvitationNotFound(Guid InvitationId);

    [ErrorCode("admin.revoke_invitation_not_pending")]
    public partial record InvitationNotPending;
}
