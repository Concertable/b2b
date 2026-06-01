using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class SimpleApplyStep : ISimpleApplyStep
{
    public Task<ApplicationEntity> ApplyAsync(int artistId, int opportunityId, ContractType contractType)
        => Task.FromResult<ApplicationEntity>(StandardApplication.Create(artistId, opportunityId, contractType));
}
