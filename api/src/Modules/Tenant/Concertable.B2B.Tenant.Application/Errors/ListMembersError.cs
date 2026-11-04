using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ListMembersError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>("You cannot view member roster for this organization.")
    };

    [ErrorCode("tenant.listmemberserror_not_permitted")]
    public partial record NotPermitted;
}
