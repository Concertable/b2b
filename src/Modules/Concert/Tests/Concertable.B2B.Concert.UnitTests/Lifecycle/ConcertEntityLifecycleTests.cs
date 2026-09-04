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
        Assert.True(concert.BeginSettlement().TryGetValue(out var operationId));
        concert.RecordSettlementReference("pi_123");
        var events = concert.DomainEvents.ToArray();

        var result = concert.Post("Changed", "Changed", 20m, 200, DateTime.UtcNow);

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(new TransitionError<ConcertState, ConcertTrigger>(ConcertState.AwaitingSettlement, ConcertTrigger.Post), error);
        Assert.Equal(ConcertState.AwaitingSettlement, concert.State);
        Assert.Equal(operationId, concert.SettlementOperationId);
        Assert.Equal("pi_123", concert.FinancialOperationReferenceId);
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
        var concert = ConcertEntity.CreateDraft(CreateBooking(), "Concert", "About", []);
        Assert.True(concert.BeginSettlement().TryGetValue(out var firstOperationId));
        concert.RecordSettlementReference("pi_failed");
        Assert.False(concert.RecordSettlementFailure("pi_failed", "declined", "Declined").IsFailure);

        var retry = concert.BeginSettlement();

        Assert.True(retry.TryGetValue(out var retryOperationId));
        Assert.Equal(firstOperationId, retryOperationId);
        Assert.Equal(retryOperationId, concert.SettlementOperationId);
        Assert.Equal("pi_failed", concert.FinancialOperationReferenceId);
        Assert.Equal(ConcertState.AwaitingSettlement, concert.State);
    }

    [Fact]
    public void RecordSettlementFailure_WhenTransitionRejected_LeavesReferenceUnset()
    {
        var concert = ConcertEntity.CreateDraft(CreateBooking(), "Concert", "About", []);

        var result = concert.RecordSettlementFailure("pi_123", "declined", "Declined");

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(new TransitionError<ConcertState, ConcertTrigger>(ConcertState.Draft, ConcertTrigger.RecordSettlementFailure), error);
        Assert.Null(concert.FinancialOperationReferenceId);
    }

    [Fact]
    public void CompleteSettlement_WhenTransitionRejected_LeavesReferenceUnset()
    {
        var concert = ConcertEntity.CreateDraft(CreateBooking(), "Concert", "About", []);
        Assert.True(concert.BeginCancellation().TryGetValue(out _));
        Assert.False(concert.Cancel().TryGetError(out _));

        var result = concert.CompleteSettlement("pi_123");

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(new TransitionError<ConcertState, ConcertTrigger>(ConcertState.Cancelled, ConcertTrigger.CompleteSettlement), error);
        Assert.Null(concert.FinancialOperationReferenceId);
    }

    [Fact]
    public void BeginSettlement_WhenRetryingAfterLaterTicketSales_ReusesReservedGross()
    {
        var concert = ConcertEntity.CreateDraft(CreateDoorSplitBooking(), "Concert", "About", []);
        concert.IncrementTicketsSold(10);
        Assert.False(concert.DeclareDoorRevenue(100m).IsFailure);
        Assert.True(concert.BeginSettlement().TryGetValue(out _));
        concert.RecordSettlementReference("pi_failed");
        Assert.False(concert.RecordSettlementFailure("pi_failed", "declined", "Declined").IsFailure);

        concert.IncrementTicketsSold(10);

        Assert.True(concert.BeginSettlement().TryGetValue(out _));
        Assert.Equal(50m, concert.SettlementGross.Amount);
    }

    private static ConfirmedBooking CreateDoorSplitBooking() => new(
        Guid.NewGuid(),
        1,
        2,
        3,
        4,
        5,
        Guid.NewGuid(),
        Guid.NewGuid(),
        DealType.DoorSplit,
        true,
        new DateTime(2030, 1, 1, 19, 0, 0, DateTimeKind.Utc),
        new DateTime(2030, 1, 1, 22, 0, 0, DateTimeKind.Utc),
        [],
        new DoorSplitTerms(50m));
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
        new FlatFeeTerms(100m));
}
