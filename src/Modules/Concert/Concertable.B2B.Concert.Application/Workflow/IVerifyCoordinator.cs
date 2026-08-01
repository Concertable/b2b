namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IVerifyCoordinator
{
    Task SucceededAsync(int applicationId, CancellationToken ct = default);
    Task FailedAsync(
        int applicationId,
        string venueManagerId,
        string? failureMessage,
        CancellationToken ct = default);
}
