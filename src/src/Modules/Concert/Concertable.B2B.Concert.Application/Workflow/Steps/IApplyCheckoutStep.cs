using Concertable.B2B.Concert.Application.Responses;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IApplyCheckoutStep : IConcertStep
{
    Task<Checkout> ExecuteAsync(int opportunityId);
}
