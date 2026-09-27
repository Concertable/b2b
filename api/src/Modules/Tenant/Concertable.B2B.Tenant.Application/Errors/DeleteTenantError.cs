using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record DeleteTenantError : IError
{
    public ErrorDefinition Definition => this switch
    {
        TenantNotFound(var tenantId) =>
            ErrorDefinition.NotFound<TenantNotFound>(
                $"Organization {tenantId} was not found."),
        CannotDeleteWithLiveObligations =>
            ErrorDefinition.Conflict<CannotDeleteWithLiveObligations>(
                "An organization with live resources or financial obligations cannot be deleted.")
    };

    [ErrorCode("tenant.delete_not_found")]
    public partial record TenantNotFound(Guid TenantId);

    [ErrorCode("tenant.delete_live_obligations")]
    public partial record CannotDeleteWithLiveObligations;
}
