using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow.Executors;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal sealed class ApplicationCancellationDispatcher : IApplicationCancellationDispatcher
{
    private readonly ICancelApplicationExecutor executor;

    public ApplicationCancellationDispatcher(ICancelApplicationExecutor executor)
    {
        this.executor = executor;
    }

    public Task CancelAsync(int applicationId) => executor.ExecuteAsync(applicationId);
}
