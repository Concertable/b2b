using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ListRolesError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>("You cannot view these roles.")
    };

    [ErrorCode("tenant.list_roles_not_permitted")]
    public partial record NotPermitted;
}
