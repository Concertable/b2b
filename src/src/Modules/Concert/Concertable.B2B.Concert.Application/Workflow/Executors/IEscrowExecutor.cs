namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IEscrowExecutor
{
    Task SucceededAsync(int applicationId, int bookingId, CancellationToken ct = default);
    Task FailedAsync(int applicationId, CancellationToken ct = default);
}
