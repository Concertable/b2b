using Concertable.Shared.Blob.Application;
using Concertable.Shared.Pdf.Application;
using QuestPDF.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Pdf;

internal sealed class PdfBlobCache : IPdfBlobCache
{
    private readonly IPdfRenderer pdfRenderer;
    private readonly IBlobStorageService blobStorage;

    public PdfBlobCache(IPdfRenderer pdfRenderer, IBlobStorageService blobStorage)
    {
        this.pdfRenderer = pdfRenderer;
        this.blobStorage = blobStorage;
    }

    public async Task<byte[]> GetOrCreateAsync(string blobName, IDocument document, CancellationToken ct = default)
    {
        if (await blobStorage.ExistsAsync(blobName))
        {
            await using var stream = await blobStorage.DownloadAsync(blobName);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }

        byte[] bytes = pdfRenderer.Render(document);

        using var upload = new MemoryStream(bytes, writable: false);
        await blobStorage.UploadAsync(upload, blobName);
        return bytes;
    }
}
