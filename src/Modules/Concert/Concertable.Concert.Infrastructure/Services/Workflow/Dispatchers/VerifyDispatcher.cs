using Concertable.Concert.Application.Workflow.Executors;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal class VerifyDispatcher : IVerifyDispatcher
{
    private readonly IVerifyExecutor executor;

    public VerifyDispatcher(IVerifyExecutor executor)
    {
        this.executor = executor;
    }

    public Task VerifyAsync(int applicationId) => executor.ExecuteAsync(applicationId);
}
