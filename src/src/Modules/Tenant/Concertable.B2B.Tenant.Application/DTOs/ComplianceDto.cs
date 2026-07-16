namespace Concertable.B2B.Tenant.Application.DTOs;

public sealed record ComplianceDto
{
    public string? VatNumber { get; init; }
    public required string SellerIdentifier { get; init; }
    public required RegisteredAddressDto RegisteredAddress { get; init; }
    public required string BankReference { get; init; }
}
