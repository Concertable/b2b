namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IEscrowExecutor
{
    Task SucceededAsync(int bookingId, CancellationToken ct = default);
    Task FailedAsync(int bookingId, CancellationToken ct = default);
}
