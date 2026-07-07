using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.Exceptions;

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

    public Task ExecuteAsync(int applicationId)
        => transitioner.TransitionAsync(applicationId, Trigger.Cancel, async app =>
        {
            // Booked + Cancel is a valid transition, but it belongs to concert-cancel — don't bypass it here.
            if (app.State is not (LifecycleState.Accepted or LifecycleState.PaymentFailed))
                throw new ConflictException($"Cannot cancel an application from {app.State}");

            await cancelStep.ExecuteAsync(app.Id);
        });
}
