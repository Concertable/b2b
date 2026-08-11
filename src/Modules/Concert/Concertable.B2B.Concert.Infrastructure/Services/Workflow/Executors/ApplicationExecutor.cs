using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;

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

    public Task<ApplicationEntity> ApplyAsync(int opportunityId, int artistId, string? paymentMethodId, ESignatureRequest eSignature) =>
        apply.ApplyAsync(opportunityId, artistId, paymentMethodId, eSignature);

    public Task AcceptAsync(int applicationId, string? paymentMethodId, ESignatureRequest eSignature) =>
        accept.AcceptAsync(applicationId, paymentMethodId, eSignature);

    public Task WithdrawAsync(int applicationId) =>
        withdraw.WithdrawAsync(applicationId);

    public Task RejectAsync(int applicationId) =>
        reject.RejectAsync(applicationId);

    public Task CancelAsync(int applicationId) =>
        cancel.CancelAsync(applicationId);
}
