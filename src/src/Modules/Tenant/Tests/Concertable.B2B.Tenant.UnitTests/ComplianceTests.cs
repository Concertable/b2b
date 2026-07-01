using Concertable.B2B.Tenant.Domain;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class ComplianceTests
{
    private static RegisteredAddress Address() =>
        new("1 High Street", null, "Manchester", "M1 1AA", "United Kingdom");

    [Fact]
    public void Constructor_WithVatRegisteredAndNumber_SetsAllValues()
    {
        var address = Address();

        var compliance = new Compliance(true, "GB123456789", "12345678", address, "GB00BANK1234");

        Assert.True(compliance.VatRegistered);
        Assert.Equal("GB123456789", compliance.VatNumber);
        Assert.Equal("12345678", compliance.SellerIdentifier);
        Assert.Equal(address, compliance.RegisteredAddress);
        Assert.Equal("GB00BANK1234", compliance.BankReference);
    }

    [Fact]
    public void Constructor_VatRegisteredWithoutNumber_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new Compliance(true, null, "12345678", Address(), "GB00BANK1234"));
    }

    [Fact]
    public void Constructor_NotVatRegisteredWithNumber_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new Compliance(false, "GB123456789", "12345678", Address(), "GB00BANK1234"));
    }

    [Fact]
    public void Constructor_NotVatRegistered_LeavesVatNumberNull()
    {
        var compliance = new Compliance(false, null, "12345678", Address(), "GB00BANK1234");

        Assert.False(compliance.VatRegistered);
        Assert.Null(compliance.VatNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_MissingSellerIdentifier_Throws(string sellerIdentifier)
    {
        Assert.Throws<DomainException>(() =>
            new Compliance(false, null, sellerIdentifier, Address(), "GB00BANK1234"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_MissingBankReference_Throws(string bankReference)
    {
        Assert.Throws<DomainException>(() =>
            new Compliance(false, null, "12345678", Address(), bankReference));
    }

    [Fact]
    public void Constructor_MissingAddress_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new Compliance(false, null, "12345678", null!, "GB00BANK1234"));
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
