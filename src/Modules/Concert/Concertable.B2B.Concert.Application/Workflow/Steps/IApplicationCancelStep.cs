namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IApplicationCancelStep
{
    Task ExecuteAsync(int applicationId);
}
