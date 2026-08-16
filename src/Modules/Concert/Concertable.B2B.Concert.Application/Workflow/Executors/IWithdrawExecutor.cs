namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IWithdrawExecutor
{
    Task<UnitResult<CancelApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default);
}
