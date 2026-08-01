namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface ICancelExecutor
{
    Task<FluentResults.Result> CancelAsync(int concertId, CancellationToken ct = default);
}
