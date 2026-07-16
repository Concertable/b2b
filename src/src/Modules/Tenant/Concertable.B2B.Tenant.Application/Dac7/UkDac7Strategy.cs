using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Tenant.Application.Dac7;

/// <summary>
/// UK (<see cref="Jurisdiction.Gb"/>) DAC7 rule. Completeness = compliance captured, seller identifier present
/// (the domain <see cref="Compliance"/> already guarantees that), and — when VAT-registered — a well-formed
/// VAT number. The VAT format is Tier-1 reference data read from <see cref="UkDac7Options"/>, not hardcoded
/// here; this is where the format check moved to from the shared org-form validator so it is jurisdiction-driven.
/// </summary>
internal sealed class UkDac7Strategy : IDac7Strategy
{
    private readonly UkDac7Options options;
    private readonly Regex vatNumberPattern;

    public UkDac7Strategy(IOptions<UkDac7Options> options)
    {
        this.options = options.Value;
        vatNumberPattern = new Regex(this.options.VatNumberPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    public bool IsComplete(Jurisdiction jurisdiction, Compliance? compliance) =>
        compliance is not null
        && !string.IsNullOrWhiteSpace(compliance.SellerIdentifier)
        && (compliance.VatNumber is null || IsValidVatNumber(jurisdiction, compliance.VatNumber));

    public bool IsValidVatNumber(Jurisdiction jurisdiction, string vatNumber) =>
        !string.IsNullOrWhiteSpace(vatNumber) && vatNumberPattern.IsMatch(vatNumber);

    public string DescribeVatNumberRequirement(Jurisdiction jurisdiction) =>
        $"{options.VatLabel} must be {options.VatNumberFormatHint}.";

    public Dac7FieldLabels GetFieldLabels(Jurisdiction jurisdiction) =>
        new(options.SellerIdentifierLabel, options.SellerIdentifierHint, options.VatLabel, options.VatNumberPlaceholder);
}
