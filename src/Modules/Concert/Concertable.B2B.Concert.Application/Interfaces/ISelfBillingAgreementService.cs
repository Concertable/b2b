using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Requests;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ISelfBillingAgreementService
{
    /// <summary>The caller's current in-force agreement, or <see langword="null"/> when none is in force.</summary>
    Task<SelfBillingAgreementDto?> GetCurrentAsync(CancellationToken ct = default);

    /// <summary>Grant or renew the caller's self-billing agreement — an append-only acceptance that freezes the
    /// supplier's identity and e-signature and opens a fresh 12-month window. Available before and after expiry.</summary>
    Task GrantAsync(ESignatureRequest eSignature, CancellationToken ct = default);

    /// <summary>The current agreement's PDF, rendered lazily on first download.</summary>
    Task<FileDownload> GetPdfAsync(CancellationToken ct = default);
}
