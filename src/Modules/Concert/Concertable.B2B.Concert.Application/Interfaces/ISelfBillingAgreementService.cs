using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Requests;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ISelfBillingAgreementService
{
    Task<SelfBillingAgreementStatusDto> GetStatusAsync(CancellationToken ct = default);

    Task<UnitResult<GrantSelfBillingAgreementError>> GrantAsync(
        ESignatureRequest eSignature,
        CancellationToken ct = default);

    Task<FileDownload> GetPdfAsync(CancellationToken ct = default);
}
