namespace Concertable.B2B.Tenant.Application.DTOs;

public sealed record ComplianceDto
{
    public required bool VatRegistered { get; init; }
    public string? VatNumber { get; init; }
    public required string SellerIdentifier { get; init; }
    public required RegisteredAddressDto RegisteredAddress { get; init; }
    public required string BankReference { get; init; }
}
