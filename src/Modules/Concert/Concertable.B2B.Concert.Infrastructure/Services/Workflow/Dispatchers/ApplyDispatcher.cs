using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

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
