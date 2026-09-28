using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ChangeBusinessActivityError : IError
{
    public ErrorDefinition Definition => this switch
    {
        TenantNotFound(var tenantId) =>
            ErrorDefinition.NotFound<TenantNotFound>($"Organization {tenantId} was not found."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The organization activity is invalid.",
                errors),
        Superseded =>
            ErrorDefinition.Conflict<Superseded>(
                "The organization activities changed. Reload them and try again."),
        NotPermitted =>
            ErrorDefinition.Forbidden<NotPermitted>(
                "The current membership cannot manage this organization.")
    };

    [ErrorCode("tenant.activity_tenant_not_found")]
    public partial record TenantNotFound(Guid TenantId);

    public partial record Invalid(ValidationErrors Errors);

    [ErrorCode("tenant.activity_superseded")]
    public partial record Superseded;
    [ErrorCode("tenant.activity_not_permitted")]
    public partial record NotPermitted;
}
