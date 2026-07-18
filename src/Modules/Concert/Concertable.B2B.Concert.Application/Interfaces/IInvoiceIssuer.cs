using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IInvoiceIssuer
{
    /// <summary>
    /// Mints the self-billed VAT invoice for a settled concert. Must run inside the Finish transition effect
    /// so the invoice, the allocated sequence number and the state change commit atomically — if any fails the
    /// whole settlement rolls back and the sweep retries it, so a settled booking always has exactly one
    /// invoice and the numbering stays gap-free. Assumes the deal is already resolved (<c>IDealAccessor</c>)
    /// and both parties' tax compliance is present (guaranteed by the Finish tax gate).
    /// </summary>
    Task IssueAsync(ConcertEntity concert, CancellationToken ct = default);
}
