namespace Concertable.B2B.Tenant.Application.Tax;

/// <summary>
/// The deployment's region tax-compliance rules: what a valid VAT number looks like for the region this
/// deployment serves (the UK regime is DAC7). One region per deployment (UK today), so the single
/// implementation is registered directly. Pure predicate only — the user-facing wording is composed in the
/// org-form validator (<c>TenantValidators</c>) from reference data; display copy is owned by the frontend.
/// </summary>
internal interface ITaxComplianceRules
{
    /// <summary>Whether <paramref name="vatNumber"/> is a well-formed VAT number for this region.</summary>
    bool IsValidVatNumber(string vatNumber);
}
