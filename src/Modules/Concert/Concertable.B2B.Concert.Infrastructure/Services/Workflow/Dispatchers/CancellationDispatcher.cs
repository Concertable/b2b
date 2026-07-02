using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using FluentResults;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal sealed class CancellationDispatcher : ICancellationDispatcher
{
    private readonly ICancelExecutor executor;

    public CancellationDispatcher(ICancelExecutor executor)
    {
        this.executor = executor;
    }

    public Task<Result> CancelAsync(int concertId) => executor.ExecuteAsync(concertId);
}
