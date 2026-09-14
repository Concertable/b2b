using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record GrantSelfBillingAgreementError : IError
{
    public ErrorDefinition Definition => this switch
    {
        MissingTenant =>
            ErrorDefinition.Forbidden<MissingTenant>("No active organization was found for the current user."),
        TenantNotFound(var tenantId) =>
            ErrorDefinition.NotFound<TenantNotFound>($"Tenant {tenantId} was not found."),
        MissingTaxCompliance =>
            ErrorDefinition.Invalid<MissingTaxCompliance>(
                "Complete your tax details before granting a self-billing agreement."),
        MissingUser =>
            ErrorDefinition.Forbidden<MissingUser>("No user was found for the current request.")
    };

    [ErrorCode("self_billing.grant.missing_tenant")]
    public partial record MissingTenant;

    [ErrorCode("self_billing.grant.tenant_not_found")]
    public partial record TenantNotFound(Guid TenantId);

    [ErrorCode("self_billing.grant.missing_tax_compliance")]
    public partial record MissingTaxCompliance;

    [ErrorCode("self_billing.grant.missing_user")]
    public partial record MissingUser;
}
