using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Tenant.Application.Tax;

/// <summary>
/// UK region tax-compliance rules — the UK's DAC7 regime. Validates VAT-number format (Tier-1 reference data
/// read from <see cref="UkTaxComplianceOptions"/>, not hardcoded here).
/// </summary>
internal sealed class UkTaxComplianceRules : ITaxComplianceRules
{
    private readonly Regex vatNumberPattern;

    public UkTaxComplianceRules(IOptions<UkTaxComplianceOptions> options) =>
        vatNumberPattern = new Regex(options.Value.VatNumberPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public bool IsValidVatNumber(string vatNumber) =>
        !string.IsNullOrWhiteSpace(vatNumber) && vatNumberPattern.IsMatch(vatNumber);
}
