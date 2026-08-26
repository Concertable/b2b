using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record UpdateTenantError : IError
{
    public ErrorDefinition Definition => this switch
    {
        TenantNotFound(var tenantId) =>
            ErrorDefinition.NotFound<TenantNotFound>(
                $"Organization {tenantId} was not found."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The organization update is invalid.",
                errors)
    };

    [ErrorCode("tenant.update_not_found")]
    public partial record TenantNotFound(Guid TenantId);

    public partial record Invalid(ValidationErrors Errors);
}
