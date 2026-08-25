namespace Concertable.B2B.Booking.Domain.Lifecycle;

public enum State
{
    AwaitingConfirmation,
    ConfirmationFailed,
    Confirmed,
    CancellationPending,
    CancellationFailed,
    Cancelled,
}
