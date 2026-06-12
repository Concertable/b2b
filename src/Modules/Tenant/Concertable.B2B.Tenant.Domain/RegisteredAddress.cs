using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain;

public sealed record RegisteredAddress
{
    public string Line1 { get; private init; } = null!;
    public string? Line2 { get; private init; }
    public string City { get; private init; } = null!;
    public string Postcode { get; private init; } = null!;
    public string Country { get; private init; } = null!;

    private RegisteredAddress() { }

    public RegisteredAddress(string line1, string? line2, string city, string postcode, string country)
    {
        DomainException.ThrowIfNullOrWhiteSpace(line1, "Line1");
        DomainException.ThrowIfNullOrWhiteSpace(city, "City");
        DomainException.ThrowIfNullOrWhiteSpace(postcode, "Postcode");
        DomainException.ThrowIfNullOrWhiteSpace(country, "Country");

        Line1 = line1;
        Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2;
        City = city;
        Postcode = postcode;
        Country = country;
    }
}
