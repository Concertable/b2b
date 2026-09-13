using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.UnitTests.Entities;

public sealed class DoorSplitDealEntityTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_ArtistDoorPercentOutsideRange_ReturnsFailure(decimal artistDoorPercent)
    {
        var result = DoorSplitDealEntity.Create(artistDoorPercent, PaymentMethod.Cash);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Create_ArtistDoorPercentAtBoundary_ReturnsDeal(decimal artistDoorPercent)
    {
        var creation = DoorSplitDealEntity.Create(artistDoorPercent, PaymentMethod.Cash);
        Assert.True(creation.TryGetValue(out var deal));

        Assert.Equal(artistDoorPercent, deal.ArtistDoorPercent);
    }

    [Fact]
    public void Update_ValidTerms_ReplacesEconomicInputs()
    {
        var creation = DoorSplitDealEntity.Create(25m, PaymentMethod.Cash);
        Assert.True(creation.TryGetValue(out var deal));

        var update = deal.Update(75m, PaymentMethod.Transfer);

        Assert.True(update.IsSuccess);
        Assert.Equal(75m, deal.ArtistDoorPercent);
        Assert.Equal(PaymentMethod.Transfer, deal.PaymentMethod);
    }
}
