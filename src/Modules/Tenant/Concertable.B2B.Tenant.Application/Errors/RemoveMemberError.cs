using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RemoveMemberError : IError
{
    public ErrorDefinition Definition => this switch
    {
        MemberNotFound(var userId) =>
            ErrorDefinition.For<RemoveMemberError>().NotFound<MemberNotFound>(
                $"User {userId} is not a member of this organization."),
        LastOwner =>
            ErrorDefinition.For<RemoveMemberError>().Conflict<LastOwner>(
                "The last owner of an organization cannot be removed.")
    };

    [ErrorCode("tenant.remove_member_not_found")]
    public partial record MemberNotFound(Guid UserId);

    [ErrorCode("tenant.remove_member_last_owner")]
    public partial record LastOwner;
}
