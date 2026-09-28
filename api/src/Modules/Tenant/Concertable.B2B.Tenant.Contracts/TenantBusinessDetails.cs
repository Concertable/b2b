namespace Concertable.B2B.Tenant.Contracts;

public sealed record TenantBusinessDetails(
    Guid TenantId,
    string LegalName,
    string ContactEmail,
    TaxComplianceDto? TaxCompliance,
    IReadOnlyList<TenantBusinessActivityKind> BusinessActivities,
    long EligibilityVersion);
