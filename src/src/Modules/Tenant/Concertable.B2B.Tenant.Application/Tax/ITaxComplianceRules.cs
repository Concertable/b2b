namespace Concertable.B2B.Tenant.Application.Tax;

/// <summary>
/// The deployment's region tax-compliance rules: what a valid VAT number looks like for the region this
/// deployment serves (the UK regime is DAC7). Region is fixed at startup (the configured
/// <see cref="Jurisdiction"/>), so the single matching implementation is registered directly. The org-form
/// write path is the consumer (VAT validation + its error message); display copy is owned by the frontend.
/// </summary>
internal interface ITaxComplianceRules
{
    /// <summary>Whether <paramref name="vatNumber"/> is a well-formed VAT number for this region.</summary>
    bool IsValidVatNumber(string vatNumber);

    /// <summary>The user-facing message describing a valid VAT number — shown when <see cref="IsValidVatNumber"/> fails, so the wording stays out of region-agnostic callers.</summary>
    string DescribeVatNumberRequirement();
}
