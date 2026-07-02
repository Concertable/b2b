using FluentResults;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface ICancelExecutor
{
    Task<Result> ExecuteAsync(int concertId);
}
