using FluentResults;

namespace Concertable.Concert.Application.Workflow.Executors;

internal interface IFinishExecutor
{
    Task<Result> ExecuteAsync(int concertId);
}
