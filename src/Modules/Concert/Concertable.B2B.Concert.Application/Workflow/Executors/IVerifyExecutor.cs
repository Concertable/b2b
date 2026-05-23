namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IVerifyExecutor
{
    Task ExecuteAsync(int applicationId);
}
