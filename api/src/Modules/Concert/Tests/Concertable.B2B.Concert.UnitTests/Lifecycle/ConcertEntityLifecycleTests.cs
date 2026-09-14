using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class ConcertEntityLifecycleTests
{
    [Fact]
    public void Post_WhenAwaitingSettlement_LeavesStateSettlementAndEventsUnchanged()
    {
        var concert = ConcertEntity.CreateDraft(CreateBooking(), new ConcertDraft("Concert", "About", []));
        Assert.True(concert.BeginSettlement().TryGetValue(out var operationId));
        var events = concert.DomainEvents.ToArray();

        var result = concert.Post("Changed", "Changed", 20m, 200, DateTime.UtcNow);

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(new TransitionError<ConcertState, ConcertTrigger>(ConcertState.AwaitingSettlement, ConcertTrigger.Post), error);
        Assert.Equal(ConcertState.AwaitingSettlement, concert.State);
        Assert.Equal(operationId, concert.SettlementOperationId);
        Assert.Equal("Concert", concert.Name);
        Assert.Equal("About", concert.About);
        Assert.Equal(0m, concert.Price);
        Assert.Equal(0, concert.TotalTickets);
        Assert.Null(concert.DatePosted);
        Assert.Equal(events, concert.DomainEvents);
    }

    [Fact]
    public void BeginSettlement_WhenPreviousAttemptFailed_ReusesTheOperation()
    {
        var concert = ConcertEntity.CreateDraft(CreateBooking(), new ConcertDraft("Concert", "About", []));
        Assert.True(concert.BeginSettlement().TryGetValue(out var firstOperationId));
        Assert.False(concert.RecordSettlementFailure("declined", "Declined").IsFailure);

        var retry = concert.BeginSettlement();

        Assert.True(retry.TryGetValue(out var retryOperationId));
        Assert.Equal(firstOperationId, retryOperationId);
        Assert.Equal(retryOperationId, concert.SettlementOperationId);
        Assert.Equal(ConcertState.AwaitingSettlement, concert.State);
    }

    [Fact]
    public void RecordSettlementFailure_WhenTransitionRejected_LeavesTheFailureUnrecorded()
    {
        var concert = ConcertEntity.CreateDraft(CreateBooking(), new ConcertDraft("Concert", "About", []));

        var result = concert.RecordSettlementFailure("declined", "Declined");

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(new TransitionError<ConcertState, ConcertTrigger>(ConcertState.Draft, ConcertTrigger.RecordSettlementFailure), error);
        Assert.Null(concert.SettlementOperationId);
    }

    [Fact]
    public void CompleteSettlement_WhenTransitionRejected_LeavesTheStateUnchanged()
    {
        var concert = ConcertEntity.CreateDraft(CreateBooking(), new ConcertDraft("Concert", "About", []));
        Assert.True(concert.BeginCancellation().TryGetValue(out _));
        Assert.False(concert.Cancel().TryGetError(out _));

        var result = concert.CompleteSettlement();

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(new TransitionError<ConcertState, ConcertTrigger>(ConcertState.Cancelled, ConcertTrigger.CompleteSettlement), error);
        Assert.Equal(ConcertState.Cancelled, concert.State);
    }

    [Fact]
    public void BeginSettlement_WhenRetryingAfterLaterTicketSales_ReusesReservedGross()
    {
        var concert = (DoorRevenueConcert)ConcertEntity.CreateDraft(
            CreateDoorSplitBooking(), new ConcertDraft("Concert", "About", []));
        concert.IncrementTicketsSold(10);
        Assert.False(concert.DeclareDoorRevenue(100m).IsFailure);
        Assert.True(concert.BeginSettlement().TryGetValue(out _));
        Assert.False(concert.RecordSettlementFailure("declined", "Declined").IsFailure);

        concert.IncrementTicketsSold(10);

        Assert.True(concert.BeginSettlement().TryGetValue(out _));
        Assert.Equal(50m, concert.SettlementGross.Amount);
    }

    private static ConfirmedBookingSnapshot CreateDoorSplitBooking() => ConfirmedBookings.DoorSplit(50m);
    private static ConfirmedBookingSnapshot CreateBooking() => ConfirmedBookings.FlatFee(100m);
}
