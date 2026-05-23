using Concertable.B2B.Concert.Application.Workflow.Executors;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

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
