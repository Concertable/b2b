namespace Concertable.B2B.Booking.Domain.Lifecycle;

internal enum BookingState
{
    AwaitingConfirmation,
    ConfirmationFailed,
    Confirmed,
    CancellationPending,
    CancellationFailed,
    Cancelled,
}
