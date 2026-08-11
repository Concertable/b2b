using Concertable.B2B.Deal.Domain.Entities;
using Concertable.Kernel;

namespace Concertable.B2B.Deal.UnitTests.Entities;

public sealed class VersusDealEntityTests
{
    [Fact]
    public void Create_NegativeGuarantee_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            VersusDealEntity.Create(-1m, 50m, PaymentMethod.Cash));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_ArtistDoorPercentOutsideRange_ThrowsDomainException(decimal artistDoorPercent)
    {
        Assert.Throws<DomainException>(() =>
            VersusDealEntity.Create(100m, artistDoorPercent, PaymentMethod.Cash));
    }

    [Fact]
    public void Update_ValidTerms_ReplacesEconomicInputs()
    {
        var deal = VersusDealEntity.Create(100m, 25m, PaymentMethod.Cash);

        deal.Update(200m, 75m, PaymentMethod.Transfer);

        Assert.Equal(200m, deal.Guarantee);
        Assert.Equal(75m, deal.ArtistDoorPercent);
        Assert.Equal(PaymentMethod.Transfer, deal.PaymentMethod);
    }
}
