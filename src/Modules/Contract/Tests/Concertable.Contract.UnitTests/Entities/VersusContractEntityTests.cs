namespace Concertable.Contract.UnitTests.Entities;
public class VersusContractEntityTests
{
    [Theory]
    [InlineData(200, 1000, 50, 700)]
    [InlineData(200, 1000, 25, 450)]
    [InlineData(0, 1000, 50, 500)]
    [InlineData(200, 0, 50, 200)]
    [InlineData(0, 0, 50, 0)]
    public void CalculateArtistShare_ShouldReturnCorrectAmount(decimal guarantee, decimal totalRevenue, decimal artistDoorPercent, decimal expected)
    {
        var contract = VersusContractEntity.Create(guarantee, artistDoorPercent, PaymentMethod.Cash);

        var result = contract.CalculateArtistShare(totalRevenue);

        Assert.Equal(expected, result);
    }
}
