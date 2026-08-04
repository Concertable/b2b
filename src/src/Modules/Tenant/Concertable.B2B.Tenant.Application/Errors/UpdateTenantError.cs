namespace Concertable.B2B.Tenant.Application.Errors;

internal sealed record UpdateTenantError(ErrorDefinition Definition) : IError
{
    internal static readonly UpdateTenantError NoActiveTenant = new(
        ErrorDefinition.Forbidden(
            "tenant.update_forbidden",
            "No active organization was found for the current user."));

    internal static UpdateTenantError NotFound(Guid tenantId) =>
        new(ErrorDefinition.NotFound(
            "tenant.update_not_found",
            $"Organization {tenantId} was not found."));
}
