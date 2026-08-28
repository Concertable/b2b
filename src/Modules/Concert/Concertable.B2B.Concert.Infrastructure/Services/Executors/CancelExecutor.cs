using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Executors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Steps;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Executors;

internal sealed class CancelExecutor : ICancelExecutor
{
    private readonly IConcertRepository concertRepository;
    private readonly IDealTypeStrategyFactory<ICancelStep> cancelStepFactory;
    private readonly IUnitOfWorkBehavior unitOfWork;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;

    public CancelExecutor(
        IConcertRepository concertRepository,
        IDealTypeStrategyFactory<ICancelStep> cancelStepFactory,
        IUnitOfWorkBehavior unitOfWork,
        IOutboxUnitOfWorkBehavior outboxBehavior)
    {
        this.concertRepository = concertRepository;
        this.cancelStepFactory = cancelStepFactory;
        this.unitOfWork = unitOfWork;
        this.outboxBehavior = outboxBehavior;
    }

    public Task<UnitResult<CancelConcertError>> CancelAsync(
        int concertId,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(
            () => outboxBehavior.ExecuteAsync(async () =>
            {
                var concert = await concertRepository.GetByIdAsync(concertId, ct);
                if (concert is null)
                    return (UnitResult<CancelConcertError>)new CancelConcertError.ConcertNotFound(concertId);
                if (concert.State is State.Cancelled or State.CancellationPending)
                    return UnitResult.Success<CancelConcertError>();
                if (concert.ValidateBeginCancellation().TryGetError(out var transitionError))
                    return new CancelConcertError.InvalidTransition(transitionError);

                await cancelStepFactory.Create(concert.DealType).ExecuteAsync(concert, ct);
                if (await concertRepository.TrySaveChangesAsync(ct))
                    return UnitResult.Success<CancelConcertError>();

                concert = await concertRepository.GetByIdAsync(concertId, ct);
                return concert?.State is State.Cancelled or State.CancellationPending
                    ? UnitResult.Success<CancelConcertError>()
                    : new CancelConcertError.Superseded(concertId);
            }, ct),
            ct);
}
