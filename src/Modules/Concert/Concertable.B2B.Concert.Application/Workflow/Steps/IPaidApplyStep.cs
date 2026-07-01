using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IPaidApplyStep : IConcertStep
{
    Task<ApplicationEntity> ApplyAsync(int artistId, int opportunityId, ContractType contractType, string paymentMethodId);
}
