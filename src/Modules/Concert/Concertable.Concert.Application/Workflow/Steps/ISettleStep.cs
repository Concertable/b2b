namespace Concertable.Concert.Application.Workflow.Steps;

internal interface ISettleStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Settled;
    Task ExecuteAsync(int bookingId);
}
