using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class ConcertEntityLifecycleTests
{
    [Fact]
    public void Post_WhenAwaitingSettlement_LeavesStateSettlementAndEventsUnchanged()
    {
        var concert = ConcertEntity.CreateDraft(CreateBooking(), "Concert", "About", []);
        Assert.False(concert.BeginSettlement("pi_123").TryGetError(out _));
        var events = concert.DomainEvents.ToArray();

        var result = concert.Post("Changed", "Changed", 20m, 200, DateTime.UtcNow);

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(new TransitionError<State, Trigger>(State.AwaitingSettlement, Trigger.Post), error);
        Assert.Equal(State.AwaitingSettlement, concert.State);
        Assert.Equal("pi_123", concert.FinancialOperationReferenceId);
        Assert.Equal("Concert", concert.Name);
        Assert.Equal("About", concert.About);
        Assert.Equal(0m, concert.Price);
        Assert.Equal(0, concert.TotalTickets);
        Assert.Null(concert.DatePosted);
        Assert.Equal(events, concert.DomainEvents);
    }

    private static ConfirmedBooking CreateBooking() => new(
        Guid.NewGuid(),
        1,
        2,
        3,
        4,
        5,
        Guid.NewGuid(),
        Guid.NewGuid(),
        DealType.FlatFee,
        false,
        new DateTime(2030, 1, 1, 19, 0, 0, DateTimeKind.Utc),
        new DateTime(2030, 1, 1, 22, 0, 0, DateTimeKind.Utc),
        [],
        new FlatFeeBookingTerms(100m));
}
