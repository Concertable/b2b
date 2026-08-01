using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union]
internal partial record AcceptInvitationError : IError
{
    partial record InvitationNotFound(Guid InvitationId);
    partial record EmailMismatch;
    partial record TenantNotFound;
    partial record AlreadyMember;
    partial record InvitationNotPending;
    partial record InvitationExpired;

    public static AcceptInvitationError NotFound(Guid invitationId) => new InvitationNotFound(invitationId);

    public static AcceptInvitationError Forbidden() => new EmailMismatch();

    public static AcceptInvitationError MissingTenant() => new TenantNotFound();

    public static AcceptInvitationError MemberConflict() => new AlreadyMember();

    public static AcceptInvitationError NotPending() => new InvitationNotPending();

    public static AcceptInvitationError Expired() => new InvitationExpired();

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        error => ErrorDefinition.NotFound(
            "tenant.accept_invitation_not_found",
            $"Invitation {error.InvitationId} was not found."),
        _ => ErrorDefinition.Forbidden(
            "tenant.accept_invitation_email_mismatch",
            "This invitation was issued to a different email address."),
        _ => ErrorDefinition.NotFound(
            "tenant.accept_invitation_tenant_not_found",
            "The organization for this invitation no longer exists."),
        _ => ErrorDefinition.Conflict(
            "tenant.accept_invitation_already_member",
            "You are already a member of this organization."),
        _ => ErrorDefinition.Conflict(
            "tenant.accept_invitation_not_pending",
            "This invitation is no longer pending."),
        _ => ErrorDefinition.Invalid(
            "tenant.accept_invitation_expired",
            "This invitation has expired."));
}
