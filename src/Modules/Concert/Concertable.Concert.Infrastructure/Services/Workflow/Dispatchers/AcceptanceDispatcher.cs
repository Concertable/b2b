using Concertable.Concert.Application.Workflow.Executors;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal class AcceptanceDispatcher : IAcceptanceDispatcher
{
    private readonly IAcceptExecutor executor;

    public AcceptanceDispatcher(IAcceptExecutor executor)
    {
        this.executor = executor;
    }

    public Task AcceptAsync(int applicationId, string? paymentMethodId)
        => executor.ExecuteAsync(applicationId, paymentMethodId);
}
