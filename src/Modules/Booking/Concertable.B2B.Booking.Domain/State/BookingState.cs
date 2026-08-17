namespace Concertable.B2B.Booking.Domain.State;

public enum BookingState
{
    AwaitingFinancialConfirmation,
    FinancialConfirmationFailed,
    Confirmed,
    CancellationPending,
    CancellationFailed,
    Cancelled,
}
