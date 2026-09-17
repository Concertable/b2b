using Concertable.B2B.Application.Domain.Lifecycle;

namespace Concertable.B2B.Application.UnitTests.Lifecycle;

public sealed class ApplicationObligationTests
{
    [Fact]
    public void EveryApplicationState_IsDeliberatelyClassified()
    {
        // Arrange
        var all = Enum.GetValues<ApplicationState>();

        // Act
        var unclassified = all.Where(state => ApplicationObligation.IsLive(state)).ToArray();

        // Assert
        Assert.Empty(unclassified);
    }

    [Fact]
    public void AcceptedIsSettled_BecauseItHandsOffToBookingInTheSameTransaction()
    {
        // Assert
        Assert.False(ApplicationObligation.IsLive(ApplicationState.Accepted));
    }
}
