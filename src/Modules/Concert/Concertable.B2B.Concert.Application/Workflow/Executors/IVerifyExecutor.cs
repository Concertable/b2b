namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IVerifyExecutor
{
    Task VerifiedAsync(int applicationId, CancellationToken ct = default);
    Task FailedAsync(int applicationId, CancellationToken ct = default);
}
