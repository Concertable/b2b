using Concertable.B2B.Concert.Application.Errors;

namespace Concertable.B2B.Concert.Application.Executors;

internal interface ICancelExecutor
{
    Task<UnitResult<CancelConcertError>> CancelAsync(
        int concertId,
        CancellationToken ct = default);
}
