using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union]
internal partial record DeleteTenantError : IError
{
    partial record TenantNotFound(Guid TenantId);

    public static DeleteTenantError NotFound(Guid tenantId) => new TenantNotFound(tenantId);

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        error => ErrorDefinition.NotFound(
            "tenant.delete_not_found",
            $"Organization {error.TenantId} was not found."));
}
