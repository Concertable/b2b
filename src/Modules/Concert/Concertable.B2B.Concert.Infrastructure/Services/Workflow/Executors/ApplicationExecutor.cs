using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class ApplicationExecutor : IApplicationExecutor
{
    private readonly IApplyExecutor apply;
    private readonly IAcceptExecutor accept;
    private readonly IWithdrawExecutor withdraw;
    private readonly IRejectExecutor reject;
    private readonly ICancelApplicationExecutor cancel;

    public ApplicationExecutor(
        IApplyExecutor apply,
        IAcceptExecutor accept,
        IWithdrawExecutor withdraw,
        IRejectExecutor reject,
        ICancelApplicationExecutor cancel)
    {
        this.apply = apply;
        this.accept = accept;
        this.withdraw = withdraw;
        this.reject = reject;
        this.cancel = cancel;
    }

    public Task<Result<ApplicationEntity, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        int artistId,
        string? paymentMethodId,
        ESignatureRequest eSignature) =>
        apply.ApplyAsync(opportunityId, artistId, paymentMethodId, eSignature);

    public Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default) =>
        accept.AcceptAsync(applicationId, paymentMethodId, eSignature, ct);

    public Task<UnitResult<CancelApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default) =>
        withdraw.WithdrawAsync(applicationId, ct);

    public Task<UnitResult<LifecycleTransitionError>> RejectAsync(int applicationId) =>
        reject.RejectAsync(applicationId);

    public Task<UnitResult<CancelApplicationError>> CancelAsync(
        int applicationId,
        CancellationToken ct = default) =>
        cancel.CancelAsync(applicationId, ct);
}
