using Reunion;

namespace Concertable.B2B.Booking.Contracts;

public interface IBookingModule
{
    Task<Option<BookingSummary>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<IReadOnlyList<BookingSummary>> GetByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds,
        CancellationToken ct = default);
    Task<Option<int>> GetContractIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<Option<ContractPdf>> GetContractPdfByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default);
}

public sealed record BookingSummary(
    int BookingId,
    int ApplicationId,
    BookingStatus Status,
    Guid OperationId,
    string? FailureCode,
    string? FailureMessage);

public sealed record ContractPdf(byte[] Content, string FileName, string ContentType);

public enum BookingStatus
{
    AwaitingConfirmation,
    ConfirmationFailed,
    Confirmed,
    CancellationPending,
    CancellationFailed,
    Cancelled,
}
