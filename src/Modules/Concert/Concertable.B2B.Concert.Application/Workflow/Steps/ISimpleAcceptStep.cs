namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface ISimpleAcceptStep : IConcertStep
{
    Task ExecuteAsync(int applicationId);
}
