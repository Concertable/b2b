using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Pdf;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class InvoicePdfRenderer : IInvoicePdfRenderer
{
    private readonly IPdfBlobCache pdfCache;

    public InvoicePdfRenderer(IPdfBlobCache pdfCache)
    {
        this.pdfCache = pdfCache;
    }

    public Task<byte[]> GetOrCreateAsync(InvoiceEntity invoice, CancellationToken ct = default)
    {
        var blobName = invoice.PdfBlobName
            ?? throw new InvalidOperationException("Invoice has no assigned PDF blob name");

        // The invoice is a pure deterministic function of the immutable snapshot, so a render now is
        // byte-identical to one at settlement — no reason to pre-generate.
        return pdfCache.GetOrCreateAsync(blobName, new InvoiceDocument(invoice), ct);
    }
}
