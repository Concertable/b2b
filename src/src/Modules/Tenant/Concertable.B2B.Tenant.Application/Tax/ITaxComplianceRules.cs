using Concertable.B2B.Tenant.Application.DTOs;

namespace Concertable.B2B.Tenant.Application.Tax;

/// <summary>
/// The deployment's tax-compliance rules: what makes a seller's tax identity complete &amp; valid for the region
/// this deployment serves (the UK regime is DAC7). The region is fixed at startup (the configured
/// <see cref="Jurisdiction"/>), so the single matching implementation is registered directly — there is no
/// per-call region dispatch. Consumers (the org-form validator; the payout gate; the dashboard nag) inject this.
/// </summary>
internal interface ITaxComplianceRules
{
    /// <summary>
    /// Whether <paramref name="taxCompliance"/> is complete &amp; valid to report a seller — null (never captured)
    /// is not complete. The single source of truth the payout gate and the dashboard nag both consume.
    /// </summary>
    bool IsComplete(TaxCompliance? taxCompliance);

    /// <summary>Whether <paramref name="vatNumber"/> is a well-formed VAT number for this region.</summary>
    bool IsValidVatNumber(string vatNumber);

    /// <summary>The user-facing message describing a valid VAT number — shown when <see cref="IsValidVatNumber"/> fails, so the wording stays out of region-agnostic callers.</summary>
    string DescribeVatNumberRequirement();

    /// <summary>The region's org-form field labels — resolved here so the org read never reads a region's options directly.</summary>
    TaxFormLabels GetFieldLabels();
}
