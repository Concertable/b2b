using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ChangeMemberRoleError : IError
{
    public ErrorDefinition Definition => this switch
    {
        MemberNotFound(var userId) =>
            ErrorDefinition.NotFound<MemberNotFound>(
                $"User {userId} is not a member of this organization."),
        LastOwner =>
            ErrorDefinition.Conflict<LastOwner>(
                "The last owner of an organization cannot be demoted.")
    };

    [ErrorCode("tenant.change_role_member_not_found")]
    public partial record MemberNotFound(Guid UserId);

    [ErrorCode("tenant.change_role_last_owner")]
    public partial record LastOwner;
}
