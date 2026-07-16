namespace Concertable.B2B.Tenant.Application.Dac7;

/// <summary>
/// Tier-1 per-region DAC7 reference data for the UK (<see cref="Jurisdiction.Gb"/>), bound from the
/// <c>Dac7:Gb</c> config section. Pure values that vary by region but drive no logic — the field labels the
/// org form renders, the VAT-number format the strategy validates against, the reporting authority. Adding a
/// region's <em>data</em> is a new config section + options class; the <em>behaviour</em> that reads it lives
/// in that region's <c>IDac7Strategy</c>. Ships with UK defaults so the module runs without config; a host
/// overrides them via the config section.
/// </summary>
public sealed class UkDac7Options
{
    /// <summary>Label for the seller-identifier field on the org form — UK sellers give a NINO or UTR.</summary>
    public string SellerIdentifierLabel { get; set; } = "National Insurance number or UTR";

    /// <summary>Helper text under the seller-identifier field on the org form.</summary>
    public string SellerIdentifierHint { get; set; } = "Companies House number, or your UTR if you're a sole trader.";

    /// <summary>Label for the VAT-number field on the org form.</summary>
    public string VatLabel { get; set; } = "VAT number";

    /// <summary>Placeholder shown in the VAT-number input on the org form.</summary>
    public string VatNumberPlaceholder { get; set; } = "GB123456789";

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
