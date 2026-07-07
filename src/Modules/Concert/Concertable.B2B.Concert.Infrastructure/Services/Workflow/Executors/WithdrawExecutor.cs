using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class WithdrawExecutor : IWithdrawExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IApplicationCancelStep cancelStep;

    public WithdrawExecutor(ILifecycleTransitioner transitioner, IApplicationCancelStep cancelStep)
    {
        this.transitioner = transitioner;
        this.cancelStep = cancelStep;
    }

    public Task ExecuteAsync(int applicationId)
        => transitioner.TransitionAsync(applicationId, Trigger.Withdraw, app =>
            app.State is LifecycleState.Accepted or LifecycleState.PaymentFailed
                ? cancelStep.ExecuteAsync(app.Id)
                : Task.CompletedTask);
}
