namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface ICancelApplicationExecutor
{
    Task<UnitResult<CancelApplicationError>> CancelAsync(int applicationId, CancellationToken ct = default);
}
