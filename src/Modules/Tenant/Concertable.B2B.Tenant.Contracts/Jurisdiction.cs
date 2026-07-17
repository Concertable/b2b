namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// A tenant's tax jurisdiction, fixed at provisioning from config (<c>TenantProvisioningOptions.DefaultJurisdiction</c>).
/// A deployment serves one region, so the matching <c>ITaxComplianceRules</c> is registered once at startup; this
/// records which region a tenant was provisioned under (the per-region VAT format, seller-id label, and reporting
/// authority all follow from it). A new region adds an enum member plus its rules + options.
/// </summary>
public enum Jurisdiction
{
    Gb = 1,
}
