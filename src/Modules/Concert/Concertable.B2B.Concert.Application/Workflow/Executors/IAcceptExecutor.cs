using Concertable.B2B.Concert.Application.Requests;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IAcceptExecutor
{
    Task ExecuteAsync(int applicationId, string? paymentMethodId, ESignatureRequest eSignature);
}
