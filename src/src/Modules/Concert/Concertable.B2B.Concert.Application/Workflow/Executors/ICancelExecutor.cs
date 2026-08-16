namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface ICancelExecutor
{
    Task<UnitResult<CancelConcertError>> CancelAsync(int concertId, CancellationToken ct = default);
}
