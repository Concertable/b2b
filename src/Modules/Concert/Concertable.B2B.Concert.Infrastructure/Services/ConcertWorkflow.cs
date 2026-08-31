using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Extensions;
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
        unitOfWorkBehavior.TryExecuteAsync(
            () => outboxBehavior.ExecuteAsync(() => CancelCoreAsync(concertId, ct), ct),
            static exception => exception.IsConcertConcurrencyConflict(),
            _ => ClassifyCancelConflictAsync(concertId, ct),
            ct);

    private async Task<UnitResult<CancelConcertError>> ClassifyCancelConflictAsync(
        int concertId,
        CancellationToken ct)
    {
        if (await concertRepository.GetStateByIdAsync(concertId, ct)
            is ConcertState.Cancelled or ConcertState.CancellationPending)
            return new Success();

        return new CancelConcertError.Superseded(concertId);
    }

    private async Task<UnitResult<CancelConcertError>> CancelCoreAsync(
        int concertId,
        CancellationToken ct)
    {
        var concert = await concertRepository.GetByIdAsync(concertId, ct);
        if (concert is null)
            return new CancelConcertError.ConcertNotFound(concertId);
        if (concert.State is ConcertState.Cancelled or ConcertState.CancellationPending)
            return new Success();
        if (concert.ValidateBeginCancellation().TryGetError(out var transitionError))
            return new CancelConcertError.InvalidTransition(transitionError);

        await cancelFactory.Create(concert.DealType).CancelAsync(concert, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new Success();
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
