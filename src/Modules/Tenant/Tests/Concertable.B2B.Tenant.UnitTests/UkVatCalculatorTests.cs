using System.Globalization;
using Concertable.B2B.Tenant.Application.Tax;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class UkVatCalculatorTests
{
    private readonly UkVatCalculator calculator;

    public UkVatCalculatorTests()
    {
        this.calculator = new UkVatCalculator();
    }

    private static decimal D(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    [Fact]
    public void Rate_IsUkStandardRate() => Assert.Equal(0.20m, calculator.Rate);

    [Theory]
    [InlineData("120", "20")]      // 100.00 net + 20.00 VAT
    [InlineData("100", "16.67")]   // 83.33 net + 16.67 VAT
    [InlineData("6", "1")]         // 5.00 net + 1.00 VAT
    [InlineData("0.15", "0.02")]   // 0.13 net + 0.02 VAT — 0.125 rounds away from zero to 0.13
    [InlineData("0", "0")]
    public void Calculate_DecomposesInclusiveGross_IntoTheVatPortion(string gross, string expectedVat) =>
        Assert.Equal(D(expectedVat), calculator.Calculate(D(gross)));

    [Theory]
    [InlineData("120")]
    [InlineData("100")]
    [InlineData("0.15")]
    [InlineData("0.01")]
    [InlineData("999.99")]
    public void Calculate_LeavesNetPlusVatEqualToGross_WithNetAsTheInclusiveDivideAt2dp(string grossText)
    {
        var gross = D(grossText);

        var vat = calculator.Calculate(gross);
        var net = gross - vat;

        Assert.Equal(gross, net + vat);                                                  // exact — no lost pennies
        Assert.Equal(Math.Round(gross / 1.2m, 2, MidpointRounding.AwayFromZero), net);   // net is the decomposed base
    }
}
