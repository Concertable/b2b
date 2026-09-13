namespace Concertable.B2B.Booking.Domain.Lifecycle;

public enum BookingTrigger
{
    Confirm,
    RecordConfirmationFailure,
    BeginCancellation,
    RecordCancellationFailure,
    Cancel
}
