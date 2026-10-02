using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.UnitTests.Lifecycle;

public sealed class BookingObligationTests
{
    [Fact]
    public void Confirmed_StaysLiveUntilConcertAcknowledgesTheHandoff()
    {
        Assert.True(BookingObligation.IsLive(BookingState.Confirmed, null));
        Assert.False(BookingObligation.IsLive(BookingState.Confirmed, DateTime.UtcNow));
    }

    [Fact]
    public void Cancelled_IsSettledRegardlessOfHandoff()
    {
        Assert.False(BookingObligation.IsLive(BookingState.Cancelled, null));
    }

    [Fact]
    public void EveryInFlightState_IsLive_SoANewStateFailsClosed()
    {
        var terminal = new[] { BookingState.Cancelled, BookingState.Confirmed };

        var inFlight = Enum.GetValues<BookingState>().Except(terminal);

        Assert.All(inFlight, state => Assert.True(BookingObligation.IsLive(state, DateTime.UtcNow)));
    }
}
