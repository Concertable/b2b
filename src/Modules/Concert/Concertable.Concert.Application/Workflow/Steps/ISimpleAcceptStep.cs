namespace Concertable.Concert.Application.Workflow.Steps;

internal interface ISimpleAcceptStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Accepted;
    Task ExecuteAsync(int applicationId);
}
