using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record UpdateRoleError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>("Only an owner can edit roles."),
        NotFound => ErrorDefinition.NotFound<NotFound>("Role was not found."),
        Protected => ErrorDefinition.Forbidden<Protected>("System presets cannot be edited."),
        Invalid => ErrorDefinition.Invalid<Invalid>("Invalid role definition."),
        NameInUse => ErrorDefinition.Conflict<NameInUse>("A role with this name already exists."),
        Superseded => ErrorDefinition.Conflict<Superseded>("The role has changed. Refresh and retry.")
    };

    [ErrorCode("tenant.update_role_not_permitted")]
    public partial record NotPermitted;
    [ErrorCode("tenant.update_role_not_found")]
    public partial record NotFound;
    [ErrorCode("tenant.update_role_protected")]
    public partial record Protected;
    [ErrorCode("tenant.update_role_invalid")]
    public partial record Invalid;
    [ErrorCode("tenant.update_role_name_in_use")]
    public partial record NameInUse;
    [ErrorCode("tenant.update_role_superseded")]
    public partial record Superseded;
}
