namespace Concertable.B2B.Booking.Domain.State;

public enum BookingState
{
    AwaitingConfirmation,
    ConfirmationFailed,
    Confirmed,
    CancellationPending,
    CancellationFailed,
    Cancelled,
}
