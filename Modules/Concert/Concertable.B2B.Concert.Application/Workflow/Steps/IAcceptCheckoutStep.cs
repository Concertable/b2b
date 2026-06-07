using Concertable.B2B.Concert.Application.Responses;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IAcceptCheckoutStep : IConcertStep
{
    Task<Checkout> ExecuteAsync(int applicationId);
}
