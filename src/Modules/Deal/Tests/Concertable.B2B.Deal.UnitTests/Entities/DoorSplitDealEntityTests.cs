using Concertable.B2B.Deal.Domain.Entities;
using Concertable.Kernel;

namespace Concertable.B2B.Deal.UnitTests.Entities;

public sealed class DoorSplitDealEntityTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_ArtistDoorPercentOutsideRange_ThrowsDomainException(decimal artistDoorPercent)
    {
        Assert.Throws<DomainException>(() =>
            DoorSplitDealEntity.Create(artistDoorPercent, PaymentMethod.Cash));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Create_ArtistDoorPercentAtBoundary_ReturnsDeal(decimal artistDoorPercent)
    {
        var deal = DoorSplitDealEntity.Create(artistDoorPercent, PaymentMethod.Cash);

        Assert.Equal(artistDoorPercent, deal.ArtistDoorPercent);
    }

    [Fact]
    public void Update_ValidTerms_ReplacesEconomicInputs()
    {
        var deal = DoorSplitDealEntity.Create(25m, PaymentMethod.Cash);

        deal.Update(75m, PaymentMethod.Transfer);

        Assert.Equal(75m, deal.ArtistDoorPercent);
        Assert.Equal(PaymentMethod.Transfer, deal.PaymentMethod);
    }
}
