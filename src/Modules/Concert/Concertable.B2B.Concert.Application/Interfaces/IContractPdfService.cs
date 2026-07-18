using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Renders the contract PDF from the immutable snapshot into the contracts/ blob prefix location assigned
/// at Accept, and reuses the stored bytes thereafter. Rendered lazily on first download (the contract is a
/// pure deterministic function of the snapshot, so on-demand rendering is byte-identical to rendering at
/// Accept and needs no tenant-less background read); a missing blob is always re-rendered, so a blob outage
/// is never fatal.
/// </summary>
internal interface IContractPdfService
{
    /// <summary>Download path: returns the stored PDF bytes, rendering + storing on first access.</summary>
    Task<byte[]> GetOrCreateAsync(ContractEntity contract, CancellationToken ct = default);
}
