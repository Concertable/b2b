namespace Concertable.B2B.Booking.Domain.Lifecycle;

internal enum State
{
    AwaitingConfirmation,
    ConfirmationFailed,
    Confirmed,
    CancellationPending,
    CancellationFailed,
    Cancelled,
}
