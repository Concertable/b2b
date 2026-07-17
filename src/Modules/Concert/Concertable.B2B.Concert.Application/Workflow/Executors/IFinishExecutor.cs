using FluentResults;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IFinishExecutor
{
    Task<Result<SettlementOutcome>> ExecuteAsync(int concertId);
}
