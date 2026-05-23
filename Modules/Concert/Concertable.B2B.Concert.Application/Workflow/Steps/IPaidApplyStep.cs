using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Enums;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IPaidApplyStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Applied;
    Task<ApplicationEntity> ApplyAsync(int artistId, int opportunityId, ContractType contractType, string paymentMethodId);
}
