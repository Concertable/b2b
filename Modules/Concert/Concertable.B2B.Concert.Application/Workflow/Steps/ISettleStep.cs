using Concertable.B2B.Concert.Domain.Enums;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface ISettleStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Settled;
    Task ExecuteAsync(int bookingId);
}
