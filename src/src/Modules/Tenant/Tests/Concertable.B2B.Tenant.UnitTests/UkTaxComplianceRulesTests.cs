using Concertable.B2B.Tenant.Application.Tax;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class UkTaxComplianceRulesTests
{
    private static UkTaxComplianceRules Rules() => new(Options.Create(new UkTaxComplianceOptions()));

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
}
