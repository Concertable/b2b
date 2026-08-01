namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface ICancelApplicationExecutor
{
    Task CancelAsync(int applicationId);
}
