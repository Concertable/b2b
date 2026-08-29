using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Requests;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationWorkflow
{
    Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        string? paymentMethodId,
        ESignatureRequest eSignature);

    Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default);
}
