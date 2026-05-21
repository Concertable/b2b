namespace Concertable.Concert.Application.Workflow.Executors;

internal interface IVerifyExecutor
{
    Task ExecuteAsync(int applicationId);
}
