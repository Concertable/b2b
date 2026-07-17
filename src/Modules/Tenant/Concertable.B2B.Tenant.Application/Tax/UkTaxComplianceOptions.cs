namespace Concertable.B2B.Tenant.Application.Tax;

/// <summary>
/// Tier-1 per-region tax-compliance reference data for the UK (<see cref="Jurisdiction.Gb"/>), whose regime is
/// DAC7, bound from the <c>TaxCompliance:Gb</c> config section. Pure values that vary by region but drive no
/// logic — the VAT-number format the rules validate against, the VAT label its error message quotes, the
/// reporting authority. Adding a region's <em>data</em> is a new config section + options class; the
/// <em>behaviour</em> that reads it lives in that region's <see cref="ITaxComplianceRules"/>. Ships with UK
/// defaults so the module runs without config; a host overrides them via the config section.
/// </summary>
public sealed class UkTaxComplianceOptions
{
    /// <summary>The VAT label, quoted in the VAT-number validation error message.</summary>
    public string VatLabel { get; set; } = "VAT number";

    /// <summary>Regex a VAT number must match: 9 or 12 digits, optionally prefixed with GB.</summary>
    public string VatNumberPattern { get; set; } = @"^(GB)?(\d{9}|\d{12})$";

    /// <summary>Human-readable description of a valid VAT number, for the validation error message.</summary>
    public string VatNumberFormatHint { get; set; } = "9 or 12 digits, optionally prefixed with GB";

    /// <summary>The tax authority DAC7 income is reported to for this region.</summary>
    public string ReportingAuthority { get; set; } = "HMRC";

    /// <summary>ISO 3166-1 alpha-2 country code carried on the annual export.</summary>
    public string IsoCountryCode { get; set; } = "GB";

    /// <summary>Reportable-from floor in minor units. DAC7 services have no de-minimis, so every paid seller is reportable from £1.</summary>
    public long ReportableFromMinorUnits { get; set; } = 100;
}
