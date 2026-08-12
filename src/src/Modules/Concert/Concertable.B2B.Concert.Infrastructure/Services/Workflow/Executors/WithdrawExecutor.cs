using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class WithdrawExecutor : IWithdrawExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IApplicationCancelStep cancelStep;
    private readonly IUnitOfWorkBehavior unitOfWork;
    private readonly IOutboxUnitOfWorkBehavior outbox;

    public WithdrawExecutor(
        ILifecycleTransitioner transitioner,
        IApplicationCancelStep cancelStep,
        IUnitOfWorkBehavior unitOfWork,
        IOutboxUnitOfWorkBehavior outbox)
    {
        this.transitioner = transitioner;
        this.cancelStep = cancelStep;
        this.unitOfWork = unitOfWork;
        this.outbox = outbox;
    }

    public async Task<UnitResult<CancelApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default)
        => await unitOfWork.ExecuteAsync(
            () => outbox.ExecuteAsync(() => WithdrawCoreAsync(applicationId, ct), ct),
            ct);

    private async Task<UnitResult<CancelApplicationError>> WithdrawCoreAsync(
        int applicationId,
        CancellationToken ct)
    {
        var transition = await transitioner.TransitionAsync<CancelApplicationError>(
            applicationId,
            Trigger.Withdraw,
            error => (CancelApplicationError)new CancelApplicationError.TransitionFailure(error),
            app => app.State is LifecycleState.Accepted or LifecycleState.PaymentFailed
                ? cancelStep.ExecuteAsync(app.Id, ct)
                : Task.FromResult(UnitResult.Success<CancelApplicationError>()),
            ct);

        return transition.Bind(_ => UnitResult.Success<CancelApplicationError>());
    }
}
