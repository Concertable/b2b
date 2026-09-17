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
    Task<int> GetArtistAwaitingCheckoutCountAsync(
        Guid artistTenantId,
        CancellationToken ct = default);

    /// <summary>GDPR erasure gate: whether any of the subject's tenants has a booking still committing money, so
    /// erasure defers rather than corrupting settlement. Fail-closed and answered tenant-less by explicit ids.</summary>
    Task<bool> HasLiveObligationsAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);

    /// <summary>The subject's portable contract fragment (GDPR arts. 15/20): the RETAINED contracts their tenants
    /// are party to — read-only, never mutated by erasure.</summary>
    Task<IReadOnlyList<SubjectContractDto>> GetSubjectContractsAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);
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
