using Concertable.B2B.Application.Domain.Lifecycle;

namespace Concertable.B2B.Application.UnitTests.Lifecycle;

public sealed class ApplicationObligationTests
{
    [Fact]
    public void EveryApplicationState_IsDeliberatelyClassified()
    {
        var all = Enum.GetValues<ApplicationState>();

        var unclassified = all.Where(state => ApplicationObligation.IsLive(state)).ToArray();

        Assert.Empty(unclassified);
    }

    [Fact]
    public void AcceptedIsSettled_BecauseItHandsOffToBookingInTheSameTransaction()
    {
        Assert.False(ApplicationObligation.IsLive(ApplicationState.Accepted));
    }
}
