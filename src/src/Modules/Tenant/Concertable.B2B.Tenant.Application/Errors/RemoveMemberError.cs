using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union]
internal partial record RemoveMemberError : IError
{
    partial record MemberNotFound(Guid UserId);
    partial record LastOwner;

    public static RemoveMemberError NotFound(Guid userId) => new MemberNotFound(userId);

    public static RemoveMemberError LastOwnerConflict() => new LastOwner();

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        error => ErrorDefinition.NotFound(
            "tenant.remove_member_not_found",
            $"User {error.UserId} is not a member of this organization."),
        _ => ErrorDefinition.Conflict(
            "tenant.remove_member_last_owner",
            "The last owner of an organization cannot be removed."));
}
