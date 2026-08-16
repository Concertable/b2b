using Concertable.B2B.Concert.Application.Requests;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IAcceptExecutor
{
    Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default);
}
