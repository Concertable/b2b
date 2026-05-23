using Concertable.Concert.Application.Responses;
using Concertable.Concert.Domain.Enums;

namespace Concertable.Concert.Application.Workflow.Steps;

internal interface IApplyCheckoutStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Applied;
    Task<Checkout> ExecuteAsync(int opportunityId);
}
