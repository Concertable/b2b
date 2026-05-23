using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Domain.Enums;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IAcceptCheckoutStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Accepted;
    Task<Checkout> ExecuteAsync(int applicationId);
}
