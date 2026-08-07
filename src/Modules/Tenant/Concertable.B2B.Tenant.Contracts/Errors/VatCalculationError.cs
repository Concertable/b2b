using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.B2B.Tenant.Contracts;

[Union(EnableImplicitConversions = false)]
public abstract partial record VatCalculationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        TenantNotFound(var tenantId) =>
            ErrorDefinition.NotFound<TenantNotFound>(
                $"Organization {tenantId} was not found.")
    };

    [ErrorCode("tenant.vat_tenant_not_found")]
    public partial record TenantNotFound(Guid TenantId);
}
