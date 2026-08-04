using Concertable.Kernel.Errors;

namespace Concertable.B2B.Tenant.Contracts;

public sealed record VatCalculationError(ErrorDefinition Definition) : IError
{
    public static VatCalculationError NotFound(Guid tenantId) =>
        new(ErrorDefinition.NotFound(
            "tenant.vat_tenant_not_found",
            $"Organization {tenantId} was not found."));
}
