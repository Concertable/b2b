using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.UnitTests.Entities;

public sealed class DoorSplitDealEntityTests
{
    [Theory]
    [InlineData(1000, 50, 500)]
    [InlineData(1000, 25, 250)]
    [InlineData(1000, 100, 1000)]
    [InlineData(0, 50, 0)]
    [InlineData(1000, 0, 0)]
    public void CalculateArtistShare_ShouldReturnCorrectAmount(decimal totalRevenue, decimal artistDoorPercent, decimal expected)
    {
        var contract = DoorSplitDealEntity.Create(artistDoorPercent, PaymentMethod.Cash);

        var result = contract.CalculateArtistShare(totalRevenue);

        Assert.Equal(expected, result);
    }
}
