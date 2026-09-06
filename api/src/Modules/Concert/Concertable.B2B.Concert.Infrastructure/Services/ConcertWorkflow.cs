using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.B2B.Deal.Contracts;
using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure.Extensions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertWorkflow : IConcertWorkflow
{
    private readonly IConcertRepository concertRepository;
    private readonly ISettlementService settlementService;
    private readonly IDealStrategyFactory<ICancelStep> cancelFactory;
    private readonly IDealStrategyFactory<ICompleteStep> completeFactory;
    private readonly IUnitOfWork unitOfWork;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;
    private readonly IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior;

    public ConcertWorkflow(
        IConcertRepository concertRepository,
        ISettlementService settlementService,
        IDealStrategyFactory<ICancelStep> cancelFactory,
        IDealStrategyFactory<ICompleteStep> completeFactory,
        IUnitOfWork unitOfWork,
        IUnitOfWorkBehavior unitOfWorkBehavior,
        IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior)
    {
        this.concertRepository = concertRepository;
        this.settlementService = settlementService;
        this.cancelFactory = cancelFactory;
        this.completeFactory = completeFactory;
        this.unitOfWork = unitOfWork;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
        this.outboxUnitOfWorkBehavior = outboxUnitOfWorkBehavior;
    }

    public Task<UnitResult<CancelConcertError>> CancelAsync(
        int concertId,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.AttemptAsync(
            () => outboxUnitOfWorkBehavior.ExecuteAsync(() => CancelCoreAsync(concertId, ct), ct),
            exception => exception.IsConcertConcurrencyConflict(concertId),
            _ => ClassifyCancelConflictAsync(concertId, ct),
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

        var executed = await completeFactory.Create(ready.DealType).CompleteAsync(ready, ct);
        if (executed.TryGetError(out var executionError))
            return executionError;

        return await settlementService.CompleteAsync(ready.ConcertId, ready.OperationId, ct);
    }

    private async Task<AttemptVerdict<UnitResult<CancelConcertError>>> ClassifyCancelConflictAsync(
        int concertId,
        CancellationToken ct)
    {
        if (await concertRepository.GetStateByIdAsync(concertId, ct)
            is ConcertState.Cancelled or ConcertState.CancellationPending)
            return new AttemptVerdict<UnitResult<CancelConcertError>>.Settled(new Success());

        return new AttemptVerdict<UnitResult<CancelConcertError>>.Unrecoverable(
            new CancelConcertError.Superseded(concertId));
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
}
