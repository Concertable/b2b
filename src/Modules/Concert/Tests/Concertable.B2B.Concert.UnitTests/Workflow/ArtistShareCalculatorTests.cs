using Concertable.B2B.Concert.Application.Workflow;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class ArtistShareCalculatorTests
{
    private readonly ArtistShareCalculator calculator;

    public ArtistShareCalculatorTests()
    {
        this.calculator = new ArtistShareCalculator(new DoorSplitCalculator(), new VersusCalculator());
    }

    [Fact]
    public void Calculate_ShouldDispatchToDoorSplitCalculator_ForDoorSplitContract()
    {
        // Arrange
        var contract = new DoorSplitContract { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70 };

        // Act
        var result = calculator.Calculate(contract, 1000);

        // Assert — matches the door-split calculator it delegates to
        Assert.Equal(new DoorSplitCalculator().Calculate(contract, 1000), result);
    }

    [Fact]
    public void Calculate_ShouldDispatchToVersusCalculator_ForVersusContract()
    {
        // Arrange
        var contract = new VersusContract { PaymentMethod = PaymentMethod.Cash, Guarantee = 200, ArtistDoorPercent = 60 };

        // Act
        var result = calculator.Calculate(contract, 1000);

        // Assert — matches the versus calculator it delegates to
        Assert.Equal(new VersusCalculator().Calculate(contract, 1000), result);
    }
}
