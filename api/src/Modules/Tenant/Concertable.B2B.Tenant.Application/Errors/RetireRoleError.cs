using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RetireRoleError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>("Only an owner can retire roles."),
        NotFound => ErrorDefinition.NotFound<NotFound>("Role was not found."),
        Protected => ErrorDefinition.Forbidden<Protected>("System presets cannot be retired."),
        InvalidReplacement => ErrorDefinition.Invalid<InvalidReplacement>("Select an active replacement role."),
        ReplacementRequired => ErrorDefinition.Conflict<ReplacementRequired>("This role is in use; select a replacement."),
        Superseded => ErrorDefinition.Conflict<Superseded>("The role has changed. Refresh and retry.")
    };

    [ErrorCode("tenant.retire_role_not_permitted")]
    public partial record NotPermitted;
    [ErrorCode("tenant.retire_role_not_found")]
    public partial record NotFound;
    [ErrorCode("tenant.retire_role_protected")]
    public partial record Protected;
    [ErrorCode("tenant.retire_role_invalid_replacement")]
    public partial record InvalidReplacement;
    [ErrorCode("tenant.retire_role_replacement_required")]
    public partial record ReplacementRequired;
    [ErrorCode("tenant.retire_role_superseded")]
    public partial record Superseded;
}
