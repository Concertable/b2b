using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class CancelExecutor : ICancelExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IDealResolver dealResolver;
    private readonly IConcertRepository concertRepository;

    public CancelExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IDealResolver dealResolver,
        IConcertRepository concertRepository)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.dealResolver = dealResolver;
        this.concertRepository = concertRepository;
    }

    public async Task<UnitResult<CancelConcertError>> CancelAsync(int concertId, CancellationToken ct = default)
    {
        var concert = await concertRepository.GetByIdWithBookingAsync(concertId, ct);
        if (concert is null)
            return UnitResult.Failure<CancelConcertError>(new CancelConcertError.ConcertNotFound(concertId));

        var transition = await transitioner.TransitionAsync<CancelConcertError>(
            concert.Booking.ApplicationId,
            Trigger.Cancel,
            error => (CancelConcertError)new CancelConcertError.TransitionFailure(error),
            async app =>
            {
                await dealResolver.ResolveByConcertIdAsync(concertId);
                var workflow = workflows.Create(app.DealType);
                var cancellation = await workflow.Cancel.ExecuteAsync(concertId, ct);
                if (cancellation.TryGetError(out var cancellationError))
                    return UnitResult.Failure(cancellationError);

                concert.Cancel();
                return UnitResult.Success<CancelConcertError>();
            }, ct);

        return transition.Bind(_ => UnitResult.Success<CancelConcertError>());
    }
}
