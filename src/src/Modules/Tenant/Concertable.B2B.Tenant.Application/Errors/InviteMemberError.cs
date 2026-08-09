using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record InviteMemberError : IError
{
    public ErrorDefinition Definition => this switch
    {
        TenantNotFound =>
            ErrorDefinition.For<InviteMemberError>().NotFound<TenantNotFound>("Your organization was not found."),
        AlreadyMember =>
            ErrorDefinition.For<InviteMemberError>().Conflict<AlreadyMember>(
                "This person is already a member of the organization."),
        InvitationPending =>
            ErrorDefinition.For<InviteMemberError>().Conflict<InvitationPending>(
                "An invitation for this email is already pending.")
    };

    [ErrorCode("tenant.invite_tenant_not_found")]
    public partial record TenantNotFound;

    [ErrorCode("tenant.invite_already_member")]
    public partial record AlreadyMember;

    [ErrorCode("tenant.invite_already_pending")]
    public partial record InvitationPending;
}
