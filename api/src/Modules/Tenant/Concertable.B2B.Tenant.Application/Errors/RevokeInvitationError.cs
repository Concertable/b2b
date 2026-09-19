using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RevokeInvitationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        InvitationNotFound(var invitationId) =>
            ErrorDefinition.NotFound<InvitationNotFound>(
                $"Invitation {invitationId} was not found."),
        InvitationNotPending =>
            ErrorDefinition.Conflict<InvitationNotPending>(
                "Only a pending invitation can be revoked."),
        NotPermitted =>
            ErrorDefinition.Forbidden<NotPermitted>(
                "You cannot revoke this invitation.")
    };

    [ErrorCode("tenant.revoke_invitation_not_found")]
    public partial record InvitationNotFound(Guid InvitationId);

    [ErrorCode("tenant.revoke_invitation_not_pending")]
    public partial record InvitationNotPending;

    [ErrorCode("tenant.revoke_invitation_not_permitted")]
    public partial record NotPermitted;
}
