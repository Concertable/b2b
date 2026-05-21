using Concertable.Concert.Application.Workflow.Executors;
using FluentResults;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal class CompletionDispatcher : ICompletionDispatcher
{
    private readonly IFinishExecutor executor;

    public CompletionDispatcher(IFinishExecutor executor)
    {
        this.executor = executor;
    }

    public Task<Result> FinishAsync(int concertId) => executor.ExecuteAsync(concertId);
}
