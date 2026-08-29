using Concertable.B2B.Concert.Application.Models;

namespace Concertable.B2B.Concert.Application.Steps;

internal interface ICompleteStep : IDealStrategy
{
    Task<Result<SettlementConfirmation, FinishConcertError>> ExecuteAsync(
        SettlementPreparation.Ready settlement,
        CancellationToken ct = default);
}