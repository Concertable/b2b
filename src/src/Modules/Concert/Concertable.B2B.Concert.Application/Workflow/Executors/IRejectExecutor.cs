namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IRejectExecutor
{
    Task RejectAsync(int applicationId);
}
