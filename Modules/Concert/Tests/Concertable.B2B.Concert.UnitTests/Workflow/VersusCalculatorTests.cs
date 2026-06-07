using Concertable.B2B.Concert.Application.Workflow;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class VersusCalculatorTests
{
    [Theory]
    [InlineData(200, 1000, 60, 800)]
    [InlineData(100, 500, 50, 350)]
    [InlineData(0, 1000, 70, 700)]
    [InlineData(150, 1000, 0, 150)]
    [InlineData(200, 0, 100, 200)]
    public void Calculate_ShouldReturnGuaranteePlusArtistDoorShare(decimal guarantee, decimal totalRevenue, decimal artistDoorPercent, decimal expected)
    {
        // Arrange
        var calculator = new VersusCalculator();
        var contract = new VersusContract { PaymentMethod = PaymentMethod.Cash, Guarantee = guarantee, ArtistDoorPercent = artistDoorPercent };

        // Act
        var result = calculator.Calculate(contract, totalRevenue);

        // Assert
        Assert.Equal(expected, result);
    }
}
