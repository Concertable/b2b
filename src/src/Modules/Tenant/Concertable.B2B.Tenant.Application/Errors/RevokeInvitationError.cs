using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union]
internal partial record RevokeInvitationError : IError
{
    partial record InvitationNotFound(Guid InvitationId);
    partial record InvitationNotPending;

    public static RevokeInvitationError NotFound(Guid invitationId) => new InvitationNotFound(invitationId);

    public static RevokeInvitationError NotPending() => new InvitationNotPending();

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        error => ErrorDefinition.NotFound(
            "tenant.revoke_invitation_not_found",
            $"Invitation {error.InvitationId} was not found."),
        _ => ErrorDefinition.Conflict(
            "tenant.revoke_invitation_not_pending",
            "Only a pending invitation can be revoked."));
}
