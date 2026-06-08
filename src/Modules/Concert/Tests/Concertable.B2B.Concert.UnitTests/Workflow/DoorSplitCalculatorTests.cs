using Concertable.B2B.Concert.Application.Workflow;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class DoorSplitCalculatorTests
{
    [Theory]
    [InlineData(1000, 70, 700)]
    [InlineData(1000, 50, 500)]
    [InlineData(1000, 100, 1000)]
    [InlineData(250, 100, 250)]
    [InlineData(1000, 0, 0)]
    [InlineData(0, 60, 0)]
    public void Calculate_ShouldReturnArtistDoorShare(decimal totalRevenue, decimal artistDoorPercent, decimal expected)
    {
        // Arrange
        var calculator = new DoorSplitCalculator();
        var contract = new DoorSplitContract { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = artistDoorPercent };

        // Act
        var result = calculator.Calculate(contract, totalRevenue);

        // Assert
        Assert.Equal(expected, result);
    }
}
