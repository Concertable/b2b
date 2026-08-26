using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ISettlementService
{
    Task<Result<SettlementPreparation, FinishConcertError>> ReserveAsync(
        int concertId,
        CancellationToken ct = default);

    Task<Result<SettlementOutcome, FinishConcertError>> CompleteAsync(
        int concertId,
        Guid operationId,
        SettlementConfirmation confirmation,
        CancellationToken ct = default);

    Task RecordFailureAsync(
        int concertId,
        Guid operationId,
        string providerReferenceId,
        string code,
        string message,
        CancellationToken ct = default);
}
