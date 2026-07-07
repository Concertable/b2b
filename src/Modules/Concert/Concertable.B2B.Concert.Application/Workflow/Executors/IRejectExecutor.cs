namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IRejectExecutor
{
    Task ExecuteAsync(int applicationId);
}
