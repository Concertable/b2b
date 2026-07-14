using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Renders the agreement PDF from the immutable snapshot, stores it once under the agreements/ blob
/// prefix, and records the blob name. Generated in the background at Accept, with a lazy fallback on
/// download (missing blob is re-rendered from the snapshot, so a blob outage is never fatal).
/// </summary>
internal interface IContractPdfService
{
    /// <summary>Download path: returns the stored PDF bytes, rendering + storing on first access.</summary>
    Task<byte[]> GetOrCreateAsync(ContractEntity agreement, CancellationToken ct = default);

    /// <summary>Background-at-Accept path: renders + stores for a just-created agreement, by booking.</summary>
    Task GenerateForBookingAsync(int bookingId, CancellationToken ct = default);
}
