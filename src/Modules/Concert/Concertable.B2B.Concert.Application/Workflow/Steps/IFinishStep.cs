namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IFinishStep : IConcertStep
{
    Task<UnitResult<FinishConcertError>> ExecuteAsync(int concertId, CancellationToken ct = default);
}
