using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Pdf;
using Concertable.Shared.Blob.Application;
using Concertable.Shared.Pdf.Application;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ContractPdfService : IContractPdfService
{
    // QuestPDF's GeneratePdf is not thread-safe: concurrent renders (e.g. the background render-at-accept
    // racing a render-on-download) corrupt the embedded font subset. Serialize every render in this process.
    private static readonly SemaphoreSlim renderLock = new(1, 1);

    private readonly IPdfService pdfService;
    private readonly IBlobStorageService blobStorage;
    private readonly IContractRepository repository;
    private readonly ILogger<ContractPdfService> logger;

    public ContractPdfService(
        IPdfService pdfService,
        IBlobStorageService blobStorage,
        IContractRepository repository,
        ILogger<ContractPdfService> logger)
    {
        this.pdfService = pdfService;
        this.blobStorage = blobStorage;
        this.repository = repository;
        this.logger = logger;
    }

    public async Task<byte[]> GetOrCreateAsync(ContractEntity agreement, CancellationToken ct = default)
    {
        var blobName = agreement.PdfBlobName
            ?? throw new InvalidOperationException("Agreement has no assigned PDF blob name");

        if (await blobStorage.ExistsAsync(blobName))
        {
            await using var stream = await blobStorage.DownloadAsync(blobName);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }

        return await RenderUploadAsync(agreement, blobName);
    }

    public async Task GenerateForBookingAsync(int bookingId, CancellationToken ct = default)
    {
        var agreement = await repository.GetByBookingIdIgnoringTenantAsync(bookingId, ct);
        if (agreement?.PdfBlobName is null || await blobStorage.ExistsAsync(agreement.PdfBlobName))
            return;

        await RenderUploadAsync(agreement, agreement.PdfBlobName);
    }

    /* Fills the location the accept transaction already assigned — never mints a name, never writes
       the DB. Idempotent: overwriting the same blob with the same rendered bytes is a no-op in effect,
       so a background render racing a lazy render can't orphan anything. */
    private async Task<byte[]> RenderUploadAsync(ContractEntity agreement, string blobName)
    {
        await renderLock.WaitAsync();
        byte[] bytes;
        try { bytes = pdfService.Render(new ContractDocument(agreement, logger)); }
        finally { renderLock.Release(); }

        using var upload = new MemoryStream(bytes, writable: false);
        await blobStorage.UploadAsync(upload, blobName);
        return bytes;
    }
}
