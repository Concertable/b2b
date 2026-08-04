using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Requests;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ISelfBillingAgreementService
{
    /// <summary>The caller's most recent agreement regardless of expiry, or <see langword="null"/> when the caller
    /// has never granted one. The Api layer derives in-force/expired status and the grant-vs-renew affordance from
    /// its expiry against now.</summary>
    Task<SelfBillingAgreementDto?> GetLatestAsync(CancellationToken ct = default);

    /// <summary>Grant or renew the caller's self-billing agreement — an append-only acceptance that freezes the
    /// supplier's identity and e-signature and opens a fresh 12-month window. Available before and after expiry.</summary>
    Task GrantAsync(ESignatureRequest eSignature, CancellationToken ct = default);

    /// <summary>The current agreement's PDF, rendered lazily on first download.</summary>
    Task<FileDownload> GetPdfAsync(CancellationToken ct = default);
}
