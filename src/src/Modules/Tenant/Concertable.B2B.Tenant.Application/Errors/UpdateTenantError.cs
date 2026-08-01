using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union]
internal partial record UpdateTenantError : IError
{
    partial record NoActiveTenant;
    partial record TenantNotFound(Guid TenantId);

    public static UpdateTenantError Forbidden() => new NoActiveTenant();

    public static UpdateTenantError NotFound(Guid tenantId) => new TenantNotFound(tenantId);

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        _ => ErrorDefinition.Forbidden(
            "tenant.update_forbidden",
            "No active organization was found for the current user."),
        error => ErrorDefinition.NotFound(
            "tenant.update_not_found",
            $"Organization {error.TenantId} was not found."));
}
