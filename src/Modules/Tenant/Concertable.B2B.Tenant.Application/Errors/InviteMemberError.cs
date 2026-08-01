using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union]
internal partial record InviteMemberError : IError
{
    partial record TenantNotFound;
    partial record AlreadyMember;
    partial record InvitationPending;

    public static InviteMemberError NotFound() => new TenantNotFound();

    public static InviteMemberError MemberConflict() => new AlreadyMember();

    public static InviteMemberError PendingConflict() => new InvitationPending();

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        _ => ErrorDefinition.NotFound(
            "tenant.invite_tenant_not_found",
            "Your organization was not found."),
        _ => ErrorDefinition.Conflict(
            "tenant.invite_already_member",
            "This person is already a member of the organization."),
        _ => ErrorDefinition.Conflict(
            "tenant.invite_already_pending",
            "An invitation for this email is already pending."));
}
