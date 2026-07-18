using Concertable.B2B.Tenant.Application.Tax;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class VatPolicyTests
{
    private readonly IVatPolicy policy;

    public VatPolicyTests()
    {
        this.policy = new VatPolicy(new UkVatCalculator());
    }

    [Fact]
    public void Apply_RegisteredSupplier_DecomposesInclusiveGross()
    {
        var result = policy.Apply(120m, "GB123456789");

        Assert.Equal(100m, result.Net);
        Assert.Equal(20m, result.Vat);
        Assert.Equal(0.20m, result.Rate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Apply_UnregisteredSupplier_ReturnsNone(string? supplierVatNumber)
    {
        var result = policy.Apply(120m, supplierVatNumber);

        Assert.Equal(120m, result.Net);
        Assert.Equal(0m, result.Vat);
        Assert.Equal(0m, result.Rate);
    }
}
