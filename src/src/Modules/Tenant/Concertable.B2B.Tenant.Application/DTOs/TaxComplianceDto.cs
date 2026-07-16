namespace Concertable.B2B.Tenant.Application.DTOs;

/// <summary>The stored tax-compliance data — mirror of the domain <c>TaxCompliance</c> VO for the org read/write.</summary>
public sealed record TaxComplianceDto
{
    public string? VatNumber { get; init; }
    public required string SellerIdentifier { get; init; }
    public required RegisteredAddressDto RegisteredAddress { get; init; }
    public required string BankReference { get; init; }
}
