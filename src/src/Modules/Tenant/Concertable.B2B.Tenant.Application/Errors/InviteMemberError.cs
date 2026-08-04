namespace Concertable.B2B.Tenant.Application.Errors;

internal sealed record InviteMemberError(ErrorDefinition Definition) : IError
{
    internal static readonly InviteMemberError TenantNotFound = new(
        ErrorDefinition.NotFound(
            "tenant.invite_tenant_not_found",
            "Your organization was not found."));

    internal static readonly InviteMemberError AlreadyMember = new(
        ErrorDefinition.Conflict(
            "tenant.invite_already_member",
            "This person is already a member of the organization."));

    internal static readonly InviteMemberError InvitationPending = new(
        ErrorDefinition.Conflict(
            "tenant.invite_already_pending",
            "An invitation for this email is already pending."));
}
