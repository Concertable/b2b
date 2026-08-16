namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IFinishExecutor
{
    Task<Result<SettlementOutcome, FinishConcertError>> FinishAsync(int concertId, CancellationToken ct = default);
}
