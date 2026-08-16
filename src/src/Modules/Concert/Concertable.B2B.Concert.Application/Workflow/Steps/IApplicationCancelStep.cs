namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IApplicationCancelStep
{
    Task<UnitResult<CancelApplicationError>> ExecuteAsync(
        int applicationId,
        CancellationToken ct = default);
}
