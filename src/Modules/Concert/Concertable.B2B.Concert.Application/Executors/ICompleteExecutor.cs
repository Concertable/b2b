using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;

namespace Concertable.B2B.Concert.Application.Executors;

internal interface ICompleteExecutor
{
    Task<Result<SettlementOutcome, FinishConcertError>> CompleteAsync(
        int concertId,
        CancellationToken ct = default);
}
