namespace Concertable.B2B.Tenant.Application.DTOs;

/// <summary>
/// The region's org-form field labels, resolved from the deployment's tax-compliance rules
/// (<c>ITaxComplianceRules.GetFieldLabels</c>). Region config, identical for every tenant — the frontend
/// renders whatever labels the read returns and never hardcodes a region's copy.
/// </summary>
public sealed record TaxFormLabels
{
    public required string SellerIdentifierLabel { get; init; }
    public required string SellerIdentifierHint { get; init; }
    public required string VatLabel { get; init; }
    public required string VatNumberPlaceholder { get; init; }
}
