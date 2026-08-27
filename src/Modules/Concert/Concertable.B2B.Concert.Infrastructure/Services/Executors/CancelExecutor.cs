using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Executors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Steps;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Executors;

internal sealed class CancelExecutor : ICancelExecutor
{
    private readonly IConcertRepository concerts;
    private readonly IDealTypeStrategyFactory<ICancelStep> steps;
    private readonly IUnitOfWorkBehavior unitOfWork;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;

    public CancelExecutor(
        IConcertRepository concerts,
        IDealTypeStrategyFactory<ICancelStep> steps,
        IUnitOfWorkBehavior unitOfWork,
        IOutboxUnitOfWorkBehavior outboxBehavior)
    {
        this.concerts = concerts;
        this.steps = steps;
        this.unitOfWork = unitOfWork;
        this.outboxBehavior = outboxBehavior;
    }

    public Task<UnitResult<CancelConcertError>> CancelAsync(
        int concertId,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(
            () => outboxBehavior.ExecuteAsync(async () =>
            {
                var concert = await concerts.GetForUpdateByIdAsync(concertId, ct);
                if (concert is null)
                    return (UnitResult<CancelConcertError>)new CancelConcertError.ConcertNotFound(concertId);
                if (concert.State is State.Cancelled or State.CancellationPending)
                    return UnitResult.Success<CancelConcertError>();
                if (concert.ValidateBeginCancellation().TryGetError(out var transitionError))
                    return new CancelConcertError.InvalidTransition(transitionError);

                await steps.Create(concert.DealType).ExecuteAsync(concert, ct);
                await concerts.SaveChangesAsync(ct);
                return UnitResult.Success<CancelConcertError>();
            }, ct),
            ct);
}
