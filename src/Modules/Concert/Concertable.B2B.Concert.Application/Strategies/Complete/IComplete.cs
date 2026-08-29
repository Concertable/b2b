using Concertable.B2B.Concert.Application.Models;

namespace Concertable.B2B.Concert.Application.Strategies;

internal interface IComplete : IDealStrategy
{
    Task<Result<SettlementConfirmation, FinishConcertError>> CompleteAsync(
        SettlementPreparation.Ready settlement,
        CancellationToken ct = default);
}
