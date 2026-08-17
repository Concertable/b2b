using Reunion;

namespace Concertable.B2B.Booking.Contracts;

public interface IBookingModule
{
    Task<Option<BookingSummary>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<Option<int>> GetContractIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default);
}

public sealed record BookingSummary(
    int BookingId,
    int ApplicationId,
    BookingStatus Status);

public enum BookingStatus
{
    AwaitingFinancialConfirmation,
    FinancialConfirmationFailed,
    Confirmed,
    CancellationPending,
    CancellationFailed,
    Cancelled,
}
