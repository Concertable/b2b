using Concertable.B2B.Tenant.Application.Dac7;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class Dac7StrategyTests
{
    private static UkDac7Strategy Uk() => new(Options.Create(new UkDac7Options()));

    private static Dac7Strategy Facade() => new(Uk());

    private static RegisteredAddress Address() =>
        new("1 High Street", null, "Manchester", "M1 1AA", "United Kingdom");

    private static Compliance Compliance(string? vatNumber) =>
        new(vatNumber, "12345678", Address(), "GB00BANK1234");

    [Fact]
    public void IsComplete_NullCompliance_IsFalse() =>
        Assert.False(Uk().IsComplete(Jurisdiction.Gb, null));

    [Fact]
    public void IsComplete_NoVatNumber_IsTrue() =>
        Assert.True(Uk().IsComplete(Jurisdiction.Gb, Compliance(null)));

    [Fact]
    public void IsComplete_ValidVatNumber_IsTrue() =>
        Assert.True(Uk().IsComplete(Jurisdiction.Gb, Compliance("GB123456789")));

    [Fact]
    public void IsComplete_InvalidVatNumber_IsFalse() =>
        Assert.False(Uk().IsComplete(Jurisdiction.Gb, Compliance("NOTAVAT")));

    [Theory]
    [InlineData("GB123456789")]
    [InlineData("123456789")]
    [InlineData("123456789012")]
    public void IsValidVatNumber_AcceptsUkFormats(string vatNumber) =>
        Assert.True(Uk().IsValidVatNumber(Jurisdiction.Gb, vatNumber));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("GB12345")]
    [InlineData("FR123456789")]
    public void IsValidVatNumber_RejectsMalformed(string vatNumber) =>
        Assert.False(Uk().IsValidVatNumber(Jurisdiction.Gb, vatNumber));

    [Fact]
    public void Facade_DelegatesToTheJurisdictionStrategy() =>
        Assert.True(Facade().IsValidVatNumber(Jurisdiction.Gb, "GB123456789"));

    [Fact]
    public void DescribeVatNumberRequirement_ComposesLabelAndHint() =>
        Assert.Equal(
            "VAT number must be 9 or 12 digits, optionally prefixed with GB.",
            Uk().DescribeVatNumberRequirement(Jurisdiction.Gb));

    [Fact]
    public void GetFieldLabels_ReturnsJurisdictionOptions()
    {
        var labels = Uk().GetFieldLabels(Jurisdiction.Gb);
        Assert.Equal("National Insurance number or UTR", labels.SellerIdentifierLabel);
        Assert.Equal("VAT number", labels.VatLabel);
        Assert.Equal("GB123456789", labels.VatNumberPlaceholder);
    }

    [Fact]
    public void Facade_UnmappedJurisdiction_Throws() =>
        Assert.Throws<KeyNotFoundException>(() => Facade().IsComplete((Jurisdiction)999, null));
}
