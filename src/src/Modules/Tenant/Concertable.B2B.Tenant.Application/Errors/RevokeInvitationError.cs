using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RevokeInvitationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        InvitationNotFound(var invitationId) =>
            ErrorDefinition.For<RevokeInvitationError>().NotFound<InvitationNotFound>(
                $"Invitation {invitationId} was not found."),
        InvitationNotPending =>
            ErrorDefinition.For<RevokeInvitationError>().Conflict<InvitationNotPending>(
                "Only a pending invitation can be revoked.")
    };

    [ErrorCode("tenant.revoke_invitation_not_found")]
    public partial record InvitationNotFound(Guid InvitationId);

    [ErrorCode("tenant.revoke_invitation_not_pending")]
    public partial record InvitationNotPending;
}
