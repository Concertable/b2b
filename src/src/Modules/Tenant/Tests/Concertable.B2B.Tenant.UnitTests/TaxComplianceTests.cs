using Concertable.B2B.Tenant.Domain;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TaxComplianceTests
{
    private static RegisteredAddress Address() =>
        new("1 High Street", null, "Manchester", "M1 1AA", "United Kingdom");

    [Fact]
    public void Constructor_SetsAllValues()
    {
        var address = Address();

        var taxCompliance = new TaxCompliance("GB123456789", "12345678", address, "GB00BANK1234");

        Assert.Equal("GB123456789", taxCompliance.VatNumber);
        Assert.Equal("12345678", taxCompliance.SellerIdentifier);
        Assert.Equal(address, taxCompliance.RegisteredAddress);
        Assert.Equal("GB00BANK1234", taxCompliance.BankReference);
    }

    [Fact]
    public void Constructor_NullVatNumber_MeansNotRegistered()
    {
        var taxCompliance = new TaxCompliance(null, "12345678", Address(), "GB00BANK1234");

        Assert.Null(taxCompliance.VatNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_BlankVatNumber_NormalizesToNull(string vatNumber)
    {
        var taxCompliance = new TaxCompliance(vatNumber, "12345678", Address(), "GB00BANK1234");

        Assert.Null(taxCompliance.VatNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_MissingSellerIdentifier_Throws(string sellerIdentifier)
    {
        Assert.Throws<DomainException>(() =>
            new TaxCompliance(null, sellerIdentifier, Address(), "GB00BANK1234"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_MissingBankReference_Throws(string bankReference)
    {
        Assert.Throws<DomainException>(() =>
            new TaxCompliance(null, "12345678", Address(), bankReference));
    }

    [Fact]
    public void Constructor_MissingAddress_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new TaxCompliance(null, "12345678", null!, "GB00BANK1234"));
    }

    [Fact]
    public void RegisteredAddress_BlankLine2_NormalizesToNull()
    {
        var address = new RegisteredAddress("1 High Street", " ", "Manchester", "M1 1AA", "United Kingdom");

        Assert.Null(address.Line2);
    }

    [Theory]
    [InlineData("", "Manchester", "M1 1AA", "United Kingdom")]
    [InlineData("1 High Street", "", "M1 1AA", "United Kingdom")]
    [InlineData("1 High Street", "Manchester", "", "United Kingdom")]
    [InlineData("1 High Street", "Manchester", "M1 1AA", "")]
    public void RegisteredAddress_MissingRequiredField_Throws(string line1, string city, string postcode, string country)
    {
        Assert.Throws<DomainException>(() =>
            new RegisteredAddress(line1, null, city, postcode, country));
    }
}
