using Concertable.Concert.Application.Responses;

namespace Concertable.Concert.Application.Workflow.Steps;

internal interface IAcceptCheckoutStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Accepted;
    Task<Checkout> ExecuteAsync(int applicationId);
}
