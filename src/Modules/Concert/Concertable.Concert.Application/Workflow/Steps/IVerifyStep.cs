namespace Concertable.Concert.Application.Workflow.Steps;

internal interface IVerifyStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Verified;
    Task ExecuteAsync(int applicationId);
}
