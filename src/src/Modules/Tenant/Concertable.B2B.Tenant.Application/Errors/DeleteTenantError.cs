namespace Concertable.B2B.Tenant.Application.Errors;

internal sealed record DeleteTenantError(ErrorDefinition Definition) : IError
{
    internal static DeleteTenantError NotFound(Guid tenantId) =>
        new(ErrorDefinition.NotFound(
            "tenant.delete_not_found",
            $"Organization {tenantId} was not found."));
}
