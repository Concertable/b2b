using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union]
internal partial record ChangeMemberRoleError : IError
{
    partial record MemberNotFound(Guid UserId);
    partial record LastOwner;

    public static ChangeMemberRoleError NotFound(Guid userId) => new MemberNotFound(userId);

    public static ChangeMemberRoleError LastOwnerConflict() => new LastOwner();

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        error => ErrorDefinition.NotFound(
            "tenant.change_role_member_not_found",
            $"User {error.UserId} is not a member of this organization."),
        _ => ErrorDefinition.Conflict(
            "tenant.change_role_last_owner",
            "The last owner of an organization cannot be demoted."));
}
