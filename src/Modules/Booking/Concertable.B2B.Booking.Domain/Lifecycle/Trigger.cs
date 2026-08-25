namespace Concertable.B2B.Booking.Domain.Lifecycle;

public enum Trigger
{
    Confirm,
    RecordConfirmationFailure,
    BeginCancellation,
    RecordCancellationFailure,
    Cancel
}
