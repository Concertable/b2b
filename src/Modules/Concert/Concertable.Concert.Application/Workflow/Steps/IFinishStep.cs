namespace Concertable.Concert.Application.Workflow.Steps;

internal interface IFinishStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Finished;
    Task ExecuteAsync(int concertId);
}
