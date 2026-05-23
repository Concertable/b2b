using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal class PaidApplyStep : IPaidApplyStep
{
    public Task<ApplicationEntity> ApplyAsync(int artistId, int opportunityId, ContractType contractType, string paymentMethodId)
        => Task.FromResult<ApplicationEntity>(PrepaidApplication.Create(artistId, opportunityId, contractType, paymentMethodId));
}
