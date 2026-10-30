using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ListInvitationsError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>("You cannot view invitations for this organization.")
    };

    [ErrorCode("tenant.listinvitationserror_not_permitted")]
    public partial record NotPermitted;
}
