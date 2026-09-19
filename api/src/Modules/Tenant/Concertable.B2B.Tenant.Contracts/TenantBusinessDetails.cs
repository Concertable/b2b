namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// The tenant facts an authorised internal consumer needs about a business it is not part of: who it is
/// legally, where to contact it, whether it is tax-ready, and which work it has activated. The single
/// Tenant-owned answer that replaces asking a marketplace profile module for a business's contact —
/// a tenant with no profile at all still has a legal name and an inbox.
/// </summary>
public sealed record BusinessFacts(
    Guid TenantId,
    string LegalName,
    string ContactEmail,
    TaxComplianceDto? TaxCompliance,
    IReadOnlyList<TenantBusinessProfileKind> BusinessProfiles,
    long AuthorityVersion);
