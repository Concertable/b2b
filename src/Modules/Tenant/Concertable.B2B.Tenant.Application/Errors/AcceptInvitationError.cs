namespace Concertable.B2B.Tenant.Application.Errors;

internal sealed record AcceptInvitationError(ErrorDefinition Definition) : IError
{
    internal static AcceptInvitationError NotFound(Guid invitationId) =>
        new(ErrorDefinition.NotFound(
            "tenant.accept_invitation_not_found",
            $"Invitation {invitationId} was not found."));

    internal static readonly AcceptInvitationError EmailMismatch = new(
        ErrorDefinition.Forbidden(
            "tenant.accept_invitation_email_mismatch",
            "This invitation was issued to a different email address."));

    internal static readonly AcceptInvitationError TenantNotFound = new(
        ErrorDefinition.NotFound(
            "tenant.accept_invitation_tenant_not_found",
            "The organization for this invitation no longer exists."));

    internal static readonly AcceptInvitationError AlreadyMember = new(
        ErrorDefinition.Conflict(
            "tenant.accept_invitation_already_member",
            "You are already a member of this organization."));

    internal static readonly AcceptInvitationError InvitationNotPending = new(
        ErrorDefinition.Conflict(
            "tenant.accept_invitation_not_pending",
            "This invitation is no longer pending."));

    internal static readonly AcceptInvitationError InvitationExpired = new(
        ErrorDefinition.Invalid(
            "tenant.accept_invitation_expired",
            "This invitation has expired."));
}
