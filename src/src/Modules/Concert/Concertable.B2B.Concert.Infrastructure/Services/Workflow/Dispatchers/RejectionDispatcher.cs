using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow.Executors;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal sealed class RejectionDispatcher : IRejectionDispatcher
{
    private readonly IRejectExecutor executor;

    public RejectionDispatcher(IRejectExecutor executor)
    {
        this.executor = executor;
    }

    public Task RejectAsync(int applicationId) => executor.ExecuteAsync(applicationId);
}
