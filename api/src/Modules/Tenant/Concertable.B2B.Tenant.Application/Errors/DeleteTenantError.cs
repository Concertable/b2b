using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record DeleteTenantError : IError
{
    public ErrorDefinition Definition => this switch
    {
        TenantNotFound(var tenantId) =>
            ErrorDefinition.NotFound<TenantNotFound>(
                $"Organization {tenantId} was not found.")
    };

    [ErrorCode("tenant.delete_not_found")]
    public partial record TenantNotFound(Guid TenantId);
}
