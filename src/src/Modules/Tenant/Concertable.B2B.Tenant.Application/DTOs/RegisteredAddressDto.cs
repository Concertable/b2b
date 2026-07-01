namespace Concertable.B2B.Tenant.Application.DTOs;

public sealed record RegisteredAddressDto
{
    public required string Line1 { get; init; }
    public string? Line2 { get; init; }
    public required string City { get; init; }
    public required string Postcode { get; init; }
    public required string Country { get; init; }
}
