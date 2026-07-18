using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Renders the self-billed invoice PDF from the immutable snapshot, stores it once under the invoices/
/// blob prefix, and reuses the stored bytes thereafter. Rendered lazily on first download (no background
/// pre-generation: an invoice is downloaded rarely, so the one-time first-download render isn't worth a
/// context-free read); a missing blob is always re-rendered from the snapshot, so a blob outage is never fatal.
/// </summary>
internal interface IInvoicePdfService
{
    /// <summary>Download path: returns the stored PDF bytes, rendering + storing on first access.</summary>
    Task<byte[]> GetOrCreateAsync(InvoiceEntity invoice, CancellationToken ct = default);
}
