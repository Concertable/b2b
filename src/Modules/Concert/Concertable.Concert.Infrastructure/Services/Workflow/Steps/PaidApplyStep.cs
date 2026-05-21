using Concertable.Concert.Application.Workflow.Steps;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Steps;

internal class PaidApplyStep : IPaidApplyStep
{
    public Task<ApplicationEntity> ApplyAsync(int artistId, int opportunityId, ContractType contractType, string paymentMethodId)
        => Task.FromResult<ApplicationEntity>(PrepaidApplication.Create(artistId, opportunityId, contractType, paymentMethodId));
}
