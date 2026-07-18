using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Pdf;
using Concertable.Shared.Blob.Application;
using Concertable.Shared.Pdf.Application;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class InvoicePdfService : IInvoicePdfService
{
    // QuestPDF's GeneratePdf is not thread-safe: two downloads racing the first render would corrupt the
    // embedded font subset. Serialize every render in this process.
    private static readonly SemaphoreSlim renderLock = new(1, 1);

    private readonly IPdfService pdfService;
    private readonly IBlobStorageService blobStorage;

    public InvoicePdfService(IPdfService pdfService, IBlobStorageService blobStorage)
    {
        this.pdfService = pdfService;
        this.blobStorage = blobStorage;
    }

    public async Task<byte[]> GetOrCreateAsync(InvoiceEntity invoice, CancellationToken ct = default)
    {
        var blobName = invoice.PdfBlobName
            ?? throw new InvalidOperationException("Invoice has no assigned PDF blob name");

        if (await blobStorage.ExistsAsync(blobName))
        {
            await using var stream = await blobStorage.DownloadAsync(blobName);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }

        /* First download renders + stores, then reuses the blob thereafter. The invoice is a pure
           deterministic function of the immutable snapshot, so a render now is byte-identical to one at
           settlement — no reason to pre-generate, and nothing here reads live tenant data. */
        await renderLock.WaitAsync(ct);
        byte[] bytes;
        try { bytes = pdfService.Render(new InvoiceDocument(invoice)); }
        finally { renderLock.Release(); }

        using var upload = new MemoryStream(bytes, writable: false);
        await blobStorage.UploadAsync(upload, blobName);
        return bytes;
    }
}
