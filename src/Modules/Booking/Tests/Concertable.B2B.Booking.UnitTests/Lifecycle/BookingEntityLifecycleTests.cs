using Concertable.B2B.Booking.Application.Mappers;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.ValueObjects;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.UnitTests;

public sealed class BookingEntityLifecycleTests
{
    [Fact]
    public void Cancel_WhenConfirmationFailed_LeavesStateFinancialFailureAndEventsUnchanged()
    {
        var acceptance = (StandardBookingAcceptance)AcceptedApplications.FlatFee().ToBookingAcceptance();
        var booking = StandardBooking.Create(acceptance);
        Assert.False(booking.RecordFinancialFailure("pi_123", "declined", "Declined").TryGetError(out _));
        var events = booking.DomainEvents.ToArray();

        var result = booking.Cancel();

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(new TransitionError<BookingState, BookingTrigger>(BookingState.ConfirmationFailed, BookingTrigger.Cancel), error);
        Assert.Equal(BookingState.ConfirmationFailed, booking.State);
        Assert.Equal("pi_123", booking.FinancialOperationReferenceId);
        Assert.Equal("declined", booking.FinancialFailureCode);
        Assert.Equal("Declined", booking.FinancialFailureMessage);
        Assert.Equal(events, booking.DomainEvents);
    }
}
