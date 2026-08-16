using Concertable.B2B.Tenant.Domain.ValueObjects;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TaxComplianceTests
{
    [Fact]
    public void Create_SetsAllValues()
    {
        var address = Address();

        var result = TaxCompliance.Create(
            "GB123456789",
            "12345678",
            address,
            "GB00BANK1234",
            true);

        Assert.True(result.TryGetValue(out var taxCompliance));
        Assert.Equal("GB123456789", taxCompliance.VatNumber);
        Assert.Equal("12345678", taxCompliance.SellerIdentifier);
        Assert.Equal(address, taxCompliance.RegisteredAddress);
        Assert.Equal("GB00BANK1234", taxCompliance.BankReference);
        Assert.True(taxCompliance.HoldsMusicLicence);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_StoresMusicLicenceAttestation(bool holdsMusicLicence)
    {
        var result = TaxCompliance.Create(
            "GB123456789",
            "12345678",
            Address(),
            "GB00BANK1234",
            holdsMusicLicence);

        Assert.True(result.TryGetValue(out var taxCompliance));
        Assert.Equal(holdsMusicLicence, taxCompliance.HoldsMusicLicence);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_MissingVatNumber_MeansNotRegistered(string? vatNumber)
    {
        var result = TaxCompliance.Create(
            vatNumber,
            "12345678",
            Address(),
            "GB00BANK1234",
            false);

        Assert.True(result.TryGetValue(out var taxCompliance));
        Assert.Null(taxCompliance.VatNumber);
    }

    [Fact]
    public void Create_InvalidFields_ReturnsStructuredErrors()
    {
        var result = TaxCompliance.Create(
            new string('V', 21),
            " ",
            null!,
            "",
            false);

        Assert.True(result.TryGetError(out var errors));
        Assert.Equal(["VatNumber must be 20 characters or fewer."], errors.Errors["VatNumber"]);
        Assert.Equal(["SellerIdentifier is required."], errors.Errors["SellerIdentifier"]);
        Assert.Equal(["RegisteredAddress is required."], errors.Errors["RegisteredAddress"]);
        Assert.Equal(["BankReference is required."], errors.Errors["BankReference"]);
    }

    [Fact]
    public void RegisteredAddress_Create_BlankLine2_NormalizesToNull()
    {
        var result = RegisteredAddress.Create(
            "1 High Street",
            " ",
            "Manchester",
            "M1 1AA",
            "United Kingdom");

        Assert.True(result.TryGetValue(out var address));
        Assert.Null(address.Line2);
    }

    [Fact]
    public void RegisteredAddress_Create_InvalidFields_ReturnsStructuredErrors()
    {
        var result = RegisteredAddress.Create(
            "",
            new string('L', 201),
            new string('C', 101),
            " ",
            new string('N', 101));

        Assert.True(result.TryGetError(out var errors));
        Assert.Equal(["Line1 is required."], errors.Errors["Line1"]);
        Assert.Equal(["Line2 must be 200 characters or fewer."], errors.Errors["Line2"]);
        Assert.Equal(["City must be 100 characters or fewer."], errors.Errors["City"]);
        Assert.Equal(["Postcode is required."], errors.Errors["Postcode"]);
        Assert.Equal(["Country must be 100 characters or fewer."], errors.Errors["Country"]);
    }

    private static RegisteredAddress Address() => RegisteredAddress
        .Create("1 High Street", null, "Manchester", "M1 1AA", "United Kingdom")
        .Match(
            address => address,
            _ => throw new InvalidOperationException("Test address is invalid."));
}
