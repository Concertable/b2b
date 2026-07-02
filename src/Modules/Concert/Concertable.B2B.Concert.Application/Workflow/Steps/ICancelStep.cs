namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface ICancelStep : IConcertStep
{
    Task ExecuteAsync(int concertId);
}
