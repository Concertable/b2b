using Concertable.Concert.Application.Workflow.Steps;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Steps;

internal class SimpleApplyStep : ISimpleApplyStep
{
    public Task<ApplicationEntity> ApplyAsync(int artistId, int opportunityId, ContractType contractType)
        => Task.FromResult<ApplicationEntity>(StandardApplication.Create(artistId, opportunityId, contractType));
}
