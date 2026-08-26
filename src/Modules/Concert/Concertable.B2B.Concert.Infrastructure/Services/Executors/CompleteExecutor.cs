using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Executors;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Steps;
using Concertable.B2B.Concert.Application.Strategies;

namespace Concertable.B2B.Concert.Infrastructure.Services.Executors;

internal sealed class CompleteExecutor : ICompleteExecutor
{
    private readonly ISettlementService settlementService;
    private readonly IConcertDealStrategyFactory<ICompleteStep> concertDealStrategyFactory;

    public CompleteExecutor(
        ISettlementService settlementService,
        IConcertDealStrategyFactory<ICompleteStep> concertDealStrategyFactory)
    {
        this.settlementService = settlementService;
        this.concertDealStrategyFactory = concertDealStrategyFactory;
    }

    public async Task<Result<SettlementOutcome, FinishConcertError>> CompleteAsync(
        int concertId,
        CancellationToken ct = default)
    {
        var prepared = await settlementService.ReserveAsync(concertId, ct);
        if (prepared.TryGetError(out var error))
            return error;
        if (!prepared.TryGetValue(out var preparation))
            throw new InvalidOperationException($"Concert {concertId} settlement preparation returned no value.");

        if (preparation is SettlementPreparation.Terminal terminal)
            return terminal.Outcome;
        if (preparation is not SettlementPreparation.Ready ready)
            throw new InvalidOperationException(
                $"Concert {concertId} returned an unknown settlement preparation.");

        var executed = await concertDealStrategyFactory
            .Create(ready.DealType)
            .ExecuteAsync(ready, ct);
        if (executed.TryGetError(out error))
            return error;
        if (!executed.TryGetValue(out var confirmation))
            throw new InvalidOperationException(
                $"Concert {concertId} settlement execution returned no confirmation.");

        return await settlementService.CompleteAsync(
            concertId,
            ready.OperationId,
            confirmation,
            ct);
    }
}
