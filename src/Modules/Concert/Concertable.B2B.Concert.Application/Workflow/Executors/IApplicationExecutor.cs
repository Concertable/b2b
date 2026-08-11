using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IApplicationExecutor
{
    Task<ApplicationEntity> ApplyAsync(int opportunityId, int artistId, string? paymentMethodId, ESignatureRequest eSignature);
    Task AcceptAsync(int applicationId, string? paymentMethodId, ESignatureRequest eSignature);
    Task WithdrawAsync(int applicationId);
    Task RejectAsync(int applicationId);
    Task CancelAsync(int applicationId);
}
