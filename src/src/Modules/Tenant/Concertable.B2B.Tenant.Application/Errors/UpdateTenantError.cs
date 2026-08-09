using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record UpdateTenantError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NoActiveTenant =>
            ErrorDefinition.For<UpdateTenantError>().Forbidden<NoActiveTenant>(
                "No active organization was found for the current user."),
        TenantNotFound(var tenantId) =>
            ErrorDefinition.For<UpdateTenantError>().NotFound<TenantNotFound>(
                $"Organization {tenantId} was not found.")
    };

    [ErrorCode("tenant.update_forbidden")]
    public partial record NoActiveTenant;

    [ErrorCode("tenant.update_not_found")]
    public partial record TenantNotFound(Guid TenantId);
}
