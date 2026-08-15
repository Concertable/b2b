using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class EscrowExecutor : IEscrowExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IApplicationCancelStep cancelStep;

    public EscrowExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IApplicationCancelStep cancelStep)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.cancelStep = cancelStep;
    }

    public async Task SucceededAsync(int applicationId, int bookingId, CancellationToken ct = default)
    {
        await transitioner.TransitionAsync(applicationId, Trigger.EscrowPaymentSucceeded, async app =>
        {
            // A late capture landing after application-cancel confirms money into escrow on a dead
            // application — compensate by refunding instead of booking.
            if (app.State == LifecycleState.Cancelled)
            {
                await cancelStep.ExecuteAsync(app.Id);
                return;
            }

            var workflow = workflows.Create(app.DealType);
            await workflow.Book.ExecuteAsync(bookingId);
        }, ct);
    }

    public async Task FailedAsync(int applicationId, CancellationToken ct = default)
    {
        await transitioner.TransitionAsync(applicationId, Trigger.EscrowPaymentFailed, ct: ct);
    }
}
