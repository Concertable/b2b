using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Tenant.Application.Tax;

/// <summary>
/// UK (<see cref="Jurisdiction.Gb"/>) region tax-compliance rules — the UK's DAC7 regime. Validates VAT-number
/// format (Tier-1 reference data read from <see cref="UkTaxComplianceOptions"/>, not hardcoded here).
/// </summary>
internal sealed class UkTaxComplianceRules : ITaxComplianceRules
{
    private readonly UkTaxComplianceOptions options;
    private readonly Regex vatNumberPattern;

    public UkTaxComplianceRules(IOptions<UkTaxComplianceOptions> options)
    {
        this.options = options.Value;
        vatNumberPattern = new Regex(this.options.VatNumberPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    public bool IsValidVatNumber(string vatNumber) =>
        !string.IsNullOrWhiteSpace(vatNumber) && vatNumberPattern.IsMatch(vatNumber);

    public string DescribeVatNumberRequirement() =>
        $"{options.VatLabel} must be {options.VatNumberFormatHint}.";
}
