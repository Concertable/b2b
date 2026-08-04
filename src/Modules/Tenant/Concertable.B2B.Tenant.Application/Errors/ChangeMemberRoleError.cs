namespace Concertable.B2B.Tenant.Application.Errors;

internal sealed record ChangeMemberRoleError(ErrorDefinition Definition) : IError
{
    internal static ChangeMemberRoleError NotFound(Guid userId) =>
        new(ErrorDefinition.NotFound(
            "tenant.change_role_member_not_found",
            $"User {userId} is not a member of this organization."));

    internal static readonly ChangeMemberRoleError LastOwner = new(
        ErrorDefinition.Conflict(
            "tenant.change_role_last_owner",
            "The last owner of an organization cannot be demoted."));
}
