using Concertable.B2B.Tenant.Application.Tax;
using Concertable.B2B.Tenant.Domain;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class UkTaxComplianceRulesTests
{
    private static UkTaxComplianceRules Rules() => new(Options.Create(new UkTaxComplianceOptions()));

    private static RegisteredAddress Address() =>
        new("1 High Street", null, "Manchester", "M1 1AA", "United Kingdom");

    private static TaxCompliance Build(string? vatNumber) =>
        new(vatNumber, "12345678", Address(), "GB00BANK1234");

    [Fact]
    public void IsComplete_NullTaxCompliance_IsFalse() =>
        Assert.False(Rules().IsComplete(null));

    [Fact]
    public void IsComplete_NoVatNumber_IsTrue() =>
        Assert.True(Rules().IsComplete(Build(null)));

    [Fact]
    public void IsComplete_ValidVatNumber_IsTrue() =>
        Assert.True(Rules().IsComplete(Build("GB123456789")));

    [Fact]
    public void IsComplete_InvalidVatNumber_IsFalse() =>
        Assert.False(Rules().IsComplete(Build("NOTAVAT")));

    [Theory]
    [InlineData("GB123456789")]
    [InlineData("123456789")]
    [InlineData("123456789012")]
    public void IsValidVatNumber_AcceptsUkFormats(string vatNumber) =>
        Assert.True(Rules().IsValidVatNumber(vatNumber));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("GB12345")]
    [InlineData("FR123456789")]
    public void IsValidVatNumber_RejectsMalformed(string vatNumber) =>
        Assert.False(Rules().IsValidVatNumber(vatNumber));

    [Fact]
    public void DescribeVatNumberRequirement_ComposesLabelAndHint() =>
        Assert.Equal(
            "VAT number must be 9 or 12 digits, optionally prefixed with GB.",
            Rules().DescribeVatNumberRequirement());

    [Fact]
    public void GetFieldLabels_ReturnsRegionOptions()
    {
        var labels = Rules().GetFieldLabels();
        Assert.Equal("National Insurance number or UTR", labels.SellerIdentifierLabel);
        Assert.Equal("VAT number", labels.VatLabel);
        Assert.Equal("GB123456789", labels.VatNumberPlaceholder);
    }
}
