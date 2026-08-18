using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Steps;

internal interface ICancelStep
{
    Task ExecuteAsync(ConcertEntity concert, CancellationToken ct = default);
}

internal interface ICompleteStep
{
    Task<UnitResult<FinishConcertError>> ExecuteAsync(
        ConcertEntity concert,
        CancellationToken ct = default);
}
