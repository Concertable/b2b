namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IFinishExecutor
{
    Task<FluentResults.Result<SettlementOutcome>> FinishAsync(int concertId, CancellationToken ct = default);
}
