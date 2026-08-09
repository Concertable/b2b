using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class CancelApplicationExecutor : ICancelApplicationExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IApplicationCancelStep cancelStep;

    public CancelApplicationExecutor(ILifecycleTransitioner transitioner, IApplicationCancelStep cancelStep)
    {
        this.transitioner = transitioner;
        this.cancelStep = cancelStep;
    }

    public async Task<UnitResult<CancelApplicationError>> CancelAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var transition = await transitioner.TransitionAsync<CancelApplicationError>(
            applicationId,
            Trigger.Cancel,
            error => (CancelApplicationError)new CancelApplicationError.TransitionFailure(error),
            async app =>
        {
            if (app.State is not (LifecycleState.Accepted or LifecycleState.PaymentFailed))
                return UnitResult.Failure<CancelApplicationError>(
                    new CancelApplicationError.InvalidState(app.State));

            return await cancelStep.ExecuteAsync(app.Id, ct);
        }, ct);

        return transition.Bind(_ => UnitResult.Success<CancelApplicationError>());
    }
}
