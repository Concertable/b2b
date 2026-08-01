using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.B2B.Tenant.Contracts;

[Union]
public partial record GetVatCalculationError : IError
{
    partial record TenantNotFound(Guid TenantId);

    public static GetVatCalculationError NotFound(Guid tenantId) => new TenantNotFound(tenantId);

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        error => ErrorDefinition.NotFound(
            "tenant.vat_tenant_not_found",
            $"Organization {error.TenantId} was not found."));
}
