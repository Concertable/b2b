using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record AcceptInvitationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        InvitationNotFound(var invitationId) =>
            ErrorDefinition.For<AcceptInvitationError>().NotFound<InvitationNotFound>(
                $"Invitation {invitationId} was not found."),
        EmailMismatch =>
            ErrorDefinition.For<AcceptInvitationError>().Forbidden<EmailMismatch>(
                "This invitation was issued to a different email address."),
        TenantNotFound =>
            ErrorDefinition.For<AcceptInvitationError>().NotFound<TenantNotFound>(
                "The organization for this invitation no longer exists."),
        AlreadyMember =>
            ErrorDefinition.For<AcceptInvitationError>().Conflict<AlreadyMember>(
                "You are already a member of this organization."),
        InvitationNotPending =>
            ErrorDefinition.For<AcceptInvitationError>().Conflict<InvitationNotPending>(
                "This invitation is no longer pending."),
        InvitationExpired =>
            ErrorDefinition.For<AcceptInvitationError>().Invalid<InvitationExpired>("This invitation has expired.")
    };

    [ErrorCode("tenant.accept_invitation_not_found")]
    public partial record InvitationNotFound(Guid InvitationId);

    [ErrorCode("tenant.accept_invitation_email_mismatch")]
    public partial record EmailMismatch;

    [ErrorCode("tenant.accept_invitation_tenant_not_found")]
    public partial record TenantNotFound;

    [ErrorCode("tenant.accept_invitation_already_member")]
    public partial record AlreadyMember;

    [ErrorCode("tenant.accept_invitation_not_pending")]
    public partial record InvitationNotPending;

    [ErrorCode("tenant.accept_invitation_expired")]
    public partial record InvitationExpired;
}
