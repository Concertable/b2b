namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IWithdrawExecutor
{
    Task ExecuteAsync(int applicationId);
}
