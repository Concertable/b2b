namespace Concertable.B2B.Tenant.Application.Errors;

internal sealed record RevokeInvitationError(ErrorDefinition Definition) : IError
{
    internal static RevokeInvitationError NotFound(Guid invitationId) =>
        new(ErrorDefinition.NotFound(
            "tenant.revoke_invitation_not_found",
            $"Invitation {invitationId} was not found."));

    internal static readonly RevokeInvitationError InvitationNotPending = new(
        ErrorDefinition.Conflict(
            "tenant.revoke_invitation_not_pending",
            "Only a pending invitation can be revoked."));
}
