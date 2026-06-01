using Concertable.B2B.Concert.Application.Workflow.Executors;
using FluentResults;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal sealed class CompletionDispatcher : ICompletionDispatcher
{
    private readonly IFinishExecutor executor;

    public CompletionDispatcher(IFinishExecutor executor)
    {
        this.executor = executor;
    }

    public Task<Result> FinishAsync(int concertId) => executor.ExecuteAsync(concertId);
}
