using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IApplyExecutor
{
    Task<Result<ApplicationEntity, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        int artistId,
        string? paymentMethodId,
        ESignatureRequest eSignature);
}
