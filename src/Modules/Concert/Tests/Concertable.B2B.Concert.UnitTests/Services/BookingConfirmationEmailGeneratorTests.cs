using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class BookingConfirmationEmailGeneratorTests
{
    private static readonly DateRange Period = new(
        new DateTime(2026, 9, 12, 19, 30, 0, DateTimeKind.Utc),
        new DateTime(2026, 9, 12, 22, 0, 0, DateTimeKind.Utc));

    private readonly IBookingConfirmationEmailGenerator generator = new BookingConfirmationEmailGenerator();

    [Fact]
    public void Generate_BothParties_RendersBothLegalNamesAndSubject()
    {
        var venue = Party("The Roundhouse", "Roundhouse Trust Ltd", Tax("GB111", Address()));
        var artist = Party("Aretha", "Aretha Live Ltd", Tax("GB222", Address()));

        var email = generator.Generate(venue, artist, Period);

        Assert.Equal("Booking confirmed: Aretha at The Roundhouse", email.Subject);
        Assert.Contains("Roundhouse Trust Ltd", email.Body);
        Assert.Contains("Aretha Live Ltd", email.Body);
        Assert.Contains("12 September 2026", email.Body);
    }

    [Fact]
    public void Generate_TaxCompliancePresent_RendersRegisteredAddressAndVat()
    {
        var venue = Party("The Roundhouse", "Roundhouse Trust Ltd", Tax("GB123456789", Address()));
        var artist = Party("Aretha", "Aretha Live Ltd", Tax("GB987654321", Address()));

        var email = generator.Generate(venue, artist, Period);

        Assert.Contains("1 High Street, Suite 4, London, EC1A 1BB, United Kingdom", email.Body);
        Assert.Contains("VAT number: GB123456789", email.Body);
        Assert.Contains("VAT number: GB987654321", email.Body);
    }

    [Fact]
    public void Generate_TaxComplianceAbsent_RendersLegalNameOnly()
    {
        var venue = Party("The Roundhouse", "Roundhouse Trust Ltd");
        var artist = Party("Aretha", "Aretha Live Ltd");

        var email = generator.Generate(venue, artist, Period);

        Assert.Contains("Roundhouse Trust Ltd", email.Body);
        Assert.Contains("Aretha Live Ltd", email.Body);
        Assert.DoesNotContain("VAT number", email.Body);
        Assert.DoesNotContain("High Street", email.Body);
    }

    [Fact]
    public void Generate_VatAbsentAddressPresent_RendersAddressOmitsVat()
    {
        var venue = Party("The Roundhouse", "Roundhouse Trust Ltd", Tax(vatNumber: null, Address(line2: null)));
        var artist = Party("Aretha", "Aretha Live Ltd", Tax(vatNumber: null, Address(line2: null)));

        var email = generator.Generate(venue, artist, Period);

        Assert.Contains("1 High Street, London, EC1A 1BB, United Kingdom", email.Body);
        Assert.DoesNotContain("VAT number", email.Body);
        Assert.DoesNotContain(", ,", email.Body);
    }

    [Fact]
    public void Generate_PlaceholderLegalName_StillRenders()
    {
        var venue = Party("The Roundhouse", "New Organisation");
        var artist = Party("Aretha", "Aretha Live Ltd", Tax("GB222", Address()));

        var email = generator.Generate(venue, artist, Period);

        Assert.Contains("New Organisation", email.Body);
    }

    [Fact]
    public void Generate_LegalDetailsWithHtml_AreEncoded()
    {
        var venue = Party("The Roundhouse", "Bar & Grill <Ltd>", Tax("GB222", Address()));
        var artist = Party("Aretha", "Aretha Live Ltd", Tax("GB333", Address()));

        var email = generator.Generate(venue, artist, Period);

        Assert.Contains("Bar &amp; Grill &lt;Ltd&gt;", email.Body);
        Assert.DoesNotContain("Bar & Grill <Ltd>", email.Body);
    }

    private static BookingConfirmationParty Party(string displayName, string legalName, TaxComplianceDto? tax = null) =>
        new(displayName, new TenantDto(Guid.NewGuid(), legalName), tax);

    private static TaxComplianceDto Tax(string? vatNumber, RegisteredAddressDto address) =>
        new()
        {
            VatNumber = vatNumber,
            SellerIdentifier = "12345678",
            RegisteredAddress = address,
            BankReference = "REF",
            HoldsMusicLicence = true
        };

    private static RegisteredAddressDto Address(string? line2 = "Suite 4") =>
        new()
        {
            Line1 = "1 High Street",
            Line2 = line2,
            City = "London",
            Postcode = "EC1A 1BB",
            Country = "United Kingdom"
        };
}
