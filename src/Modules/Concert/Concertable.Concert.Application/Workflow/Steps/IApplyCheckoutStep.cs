using Concertable.Concert.Application.Responses;

namespace Concertable.Concert.Application.Workflow.Steps;

internal interface IApplyCheckoutStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Applied;
    Task<Checkout> ExecuteAsync(int opportunityId);
}
