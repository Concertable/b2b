using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IApplicationExecutor
{
    Task<Result<ApplicationEntity, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        int artistId,
        string? paymentMethodId,
        ESignatureRequest eSignature);
    Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default);
    Task<UnitResult<CancelApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<UnitResult<LifecycleTransitionError>> RejectAsync(int applicationId);
    Task<UnitResult<CancelApplicationError>> CancelAsync(
        int applicationId,
        CancellationToken ct = default);
}
