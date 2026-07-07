namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface ICancelApplicationExecutor
{
    Task ExecuteAsync(int applicationId);
}
