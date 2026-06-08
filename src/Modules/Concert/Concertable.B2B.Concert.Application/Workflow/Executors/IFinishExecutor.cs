using FluentResults;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IFinishExecutor
{
    Task<Result> ExecuteAsync(int concertId);
}
