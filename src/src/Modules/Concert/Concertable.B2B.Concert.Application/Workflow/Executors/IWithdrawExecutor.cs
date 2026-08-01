namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IWithdrawExecutor
{
    Task WithdrawAsync(int applicationId);
}
