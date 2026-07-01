namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IFinishStep : IConcertStep
{
    Task ExecuteAsync(int concertId);
}
