namespace Concertable.B2B.Tenant.Application.Errors;

internal sealed record RemoveMemberError(ErrorDefinition Definition) : IError
{
    internal static RemoveMemberError NotFound(Guid userId) =>
        new(ErrorDefinition.NotFound(
            "tenant.remove_member_not_found",
            $"User {userId} is not a member of this organization."));

    internal static readonly RemoveMemberError LastOwner = new(
        ErrorDefinition.Conflict(
            "tenant.remove_member_last_owner",
            "The last owner of an organization cannot be removed."));
}
