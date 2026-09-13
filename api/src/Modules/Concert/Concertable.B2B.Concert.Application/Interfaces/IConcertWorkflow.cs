using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertWorkflow
{
    Task<UnitResult<CancelConcertError>> CancelAsync(
        int concertId,
        CancellationToken ct = default);

    Task<Result<SettlementOutcome, FinishConcertError>> CompleteAsync(
        int concertId,
        CancellationToken ct = default);
}
