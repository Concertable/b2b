using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record InviteMemberError : IError
{
    public ErrorDefinition Definition => this switch
    {
        TenantNotFound =>
            ErrorDefinition.NotFound<TenantNotFound>("Your organization was not found."),
        AlreadyMember =>
            ErrorDefinition.Conflict<AlreadyMember>(
                "This person is already a member of the organization."),
        InvitationPending =>
            ErrorDefinition.Conflict<InvitationPending>(
                "An invitation for this email is already pending."),
        Unauthenticated =>
            ErrorDefinition.Forbidden<Unauthenticated>("No authenticated user was found.")
    };

    [ErrorCode("tenant.invite_tenant_not_found")]
    public partial record TenantNotFound;

    [ErrorCode("tenant.invite_already_member")]
    public partial record AlreadyMember;

    [ErrorCode("tenant.invite_already_pending")]
    public partial record InvitationPending;

    [ErrorCode("tenant.invite_unauthenticated")]
    public partial record Unauthenticated;
}
