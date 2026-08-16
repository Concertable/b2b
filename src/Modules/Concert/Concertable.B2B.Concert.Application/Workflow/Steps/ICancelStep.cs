namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface ICancelStep : IConcertStep
{
    Task<UnitResult<CancelConcertError>> ExecuteAsync(int concertId, CancellationToken ct = default);
}
