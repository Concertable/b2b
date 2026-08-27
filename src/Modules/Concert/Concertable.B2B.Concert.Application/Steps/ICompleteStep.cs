using Concertable.B2B.Concert.Application.Models;

namespace Concertable.B2B.Concert.Application.Steps;

internal interface ICompleteStep
{
    Task<Result<SettlementConfirmation, FinishConcertError>> ExecuteAsync(
        SettlementPreparation.Ready settlement,
        CancellationToken ct = default);
}