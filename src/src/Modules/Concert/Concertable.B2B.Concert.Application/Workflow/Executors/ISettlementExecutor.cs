namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface ISettlementExecutor
{
    Task SucceededAsync(int bookingId, CancellationToken ct = default);
    Task FailedAsync(int bookingId, CancellationToken ct = default);
}
