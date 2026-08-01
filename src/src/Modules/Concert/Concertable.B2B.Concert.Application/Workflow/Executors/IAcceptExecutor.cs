using Concertable.B2B.Concert.Application.Requests;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IAcceptExecutor
{
    Task AcceptAsync(int applicationId, string? paymentMethodId, ESignatureRequest eSignature);
}
