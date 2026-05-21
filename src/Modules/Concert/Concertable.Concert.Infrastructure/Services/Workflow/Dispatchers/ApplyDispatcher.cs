using Concertable.Concert.Application.Workflow.Executors;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal class ApplyDispatcher : IApplyDispatcher
{
    private readonly IApplyExecutor executor;

    public ApplyDispatcher(IApplyExecutor executor)
    {
        this.executor = executor;
    }

    public Task<ApplicationEntity> ApplyAsync(int opportunityId, int artistId)
        => executor.ExecuteAsync(opportunityId, artistId, null);

    public Task<ApplicationEntity> ApplyAsync(int opportunityId, int artistId, string paymentMethodId)
        => executor.ExecuteAsync(opportunityId, artistId, paymentMethodId);
}
