using Concertable.B2B.Concert.Application.Workflow.Executors;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal sealed class VerifyDispatcher : IVerifyDispatcher
{
    private readonly IVerifyExecutor executor;

    public VerifyDispatcher(IVerifyExecutor executor)
    {
        this.executor = executor;
    }

    public Task VerifyAsync(int applicationId) => executor.ExecuteAsync(applicationId);
}
