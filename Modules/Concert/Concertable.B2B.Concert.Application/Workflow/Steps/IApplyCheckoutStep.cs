using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Domain.Enums;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IApplyCheckoutStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Applied;
    Task<Checkout> ExecuteAsync(int opportunityId);
}
