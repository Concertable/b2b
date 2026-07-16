using System.Text.RegularExpressions;
using Concertable.B2B.Tenant.Application.DTOs;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Tenant.Application.Tax;

/// <summary>
/// UK (<see cref="Jurisdiction.Gb"/>) tax-compliance rules — the UK's DAC7 regime. Completeness = tax details
/// captured, seller identifier present (the domain <see cref="TaxCompliance"/> already guarantees that), and — when
/// VAT-registered — a well-formed VAT number. The VAT format is Tier-1 reference data read from
/// <see cref="UkTaxComplianceOptions"/>, not hardcoded here.
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

    public bool IsComplete(TaxCompliance? taxCompliance) =>
        taxCompliance is not null
        && !string.IsNullOrWhiteSpace(taxCompliance.SellerIdentifier)
        && (taxCompliance.VatNumber is null || IsValidVatNumber(taxCompliance.VatNumber));

    public bool IsValidVatNumber(string vatNumber) =>
        !string.IsNullOrWhiteSpace(vatNumber) && vatNumberPattern.IsMatch(vatNumber);

    public string DescribeVatNumberRequirement() =>
        $"{options.VatLabel} must be {options.VatNumberFormatHint}.";

    public TaxFormLabels GetFieldLabels() => new()
    {
        SellerIdentifierLabel = options.SellerIdentifierLabel,
        SellerIdentifierHint = options.SellerIdentifierHint,
        VatLabel = options.VatLabel,
        VatNumberPlaceholder = options.VatNumberPlaceholder,
    };
}
