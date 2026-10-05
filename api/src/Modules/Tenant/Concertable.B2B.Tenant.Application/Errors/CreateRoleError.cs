using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CreateRoleError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>("Only an owner can create roles."),
        Invalid => ErrorDefinition.Invalid<Invalid>("Invalid role definition."),
        NameInUse => ErrorDefinition.Conflict<NameInUse>("A role with this name already exists.")
    };

    [ErrorCode("tenant.create_role_not_permitted")]
    public partial record NotPermitted;
    [ErrorCode("tenant.create_role_invalid")]
    public partial record Invalid;
    [ErrorCode("tenant.create_role_name_in_use")]
    public partial record NameInUse;
}
