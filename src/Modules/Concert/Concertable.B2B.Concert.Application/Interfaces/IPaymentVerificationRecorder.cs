namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IPaymentVerificationRecorder
{
    Task RecordVerifiedAsync(int applicationId, CancellationToken ct = default);
    Task RecordFailedAsync(int applicationId, string venueManagerId, string? failureMessage, CancellationToken ct = default);
}
