using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertWorkflow : IConcertWorkflow
{
    private readonly IConcertRepository concertRepository;
    private readonly ISettlementService settlementService;
    private readonly IDealStrategyFactory<ICancel> cancelFactory;
    private readonly IDealStrategyFactory<IComplete> completeFactory;
    private readonly IUnitOfWork unitOfWork;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;

    public ConcertWorkflow(
        IConcertRepository concertRepository,
        ISettlementService settlementService,
        IDealStrategyFactory<ICancel> cancelFactory,
        IDealStrategyFactory<IComplete> completeFactory,
        IUnitOfWork unitOfWork,
        IUnitOfWorkBehavior unitOfWorkBehavior,
        IOutboxUnitOfWorkBehavior outboxBehavior)
    {
        this.concertRepository = concertRepository;
        this.settlementService = settlementService;
        this.cancelFactory = cancelFactory;
        this.completeFactory = completeFactory;
        this.unitOfWork = unitOfWork;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
        this.outboxBehavior = outboxBehavior;
    }

    public Task<UnitResult<CancelConcertError>> CancelAsync(
        int concertId,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.ExecuteAsync(
            () => outboxBehavior.ExecuteAsync(async () =>
            {
                var concert = await concertRepository.GetByIdAsync(concertId, ct);
                if (concert is null)
                    return (UnitResult<CancelConcertError>)new CancelConcertError.ConcertNotFound(concertId);
                if (concert.State is ConcertState.Cancelled or ConcertState.CancellationPending)
                    return UnitResult.Success<CancelConcertError>();
                if (concert.ValidateBeginCancellation().TryGetError(out var transitionError))
                    return new CancelConcertError.InvalidTransition(transitionError);

                await cancelFactory.Create(concert.DealType).CancelAsync(concert, ct);
                if (await unitOfWork.TrySaveChangesAsync(
                        static exception => exception is DbUpdateConcurrencyException,
                        ct))
                    return UnitResult.Success<CancelConcertError>();

                concert = await concertRepository.GetByIdAsync(concertId, ct);
                return concert?.State is ConcertState.Cancelled or ConcertState.CancellationPending
                    ? UnitResult.Success<CancelConcertError>()
                    : new CancelConcertError.Superseded(concertId);
            }, ct),
            ct);

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

        var executed = await completeFactory
            .Create(ready.DealType)
            .CompleteAsync(ready, ct);
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
