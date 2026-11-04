using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ChangeMemberRolesError : IError
{
    public ErrorDefinition Definition => this switch
    {
        MemberNotFound(var userId) =>
            ErrorDefinition.NotFound<MemberNotFound>(
                $"User {userId} is not a member of this organization."),
        LastOwner =>
            ErrorDefinition.Conflict<LastOwner>(
                "The last owner of an organization cannot be demoted."),
        InvalidRoles =>
            ErrorDefinition.Invalid<InvalidRoles>("Select active roles in this organization."),
        NotPermitted =>
            ErrorDefinition.Forbidden<NotPermitted>(
                "Only an owner can change member roles."),
        RoleNotAvailableForTenant =>
            ErrorDefinition.Invalid<RoleNotAvailableForTenant>(
                "This role is not available for your organization's activities.")
    };

    [ErrorCode("tenant.change_role_member_not_found")]
    public partial record MemberNotFound(Guid UserId);

    [ErrorCode("tenant.change_role_last_owner")]
    public partial record LastOwner;

    [ErrorCode("tenant.change_role_invalid_roles")]
    public partial record InvalidRoles;

    [ErrorCode("tenant.change_role_not_permitted")]
    public partial record NotPermitted;

    [ErrorCode("tenant.change_role_role_not_available_for_tenant")]
    public partial record RoleNotAvailableForTenant;
}
