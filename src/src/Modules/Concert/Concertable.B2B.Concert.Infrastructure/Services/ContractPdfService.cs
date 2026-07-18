using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Pdf;
using Concertable.Shared.Blob.Application;
using Concertable.Shared.Pdf.Application;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ContractPdfService : IContractPdfService
{
    // QuestPDF's GeneratePdf is not thread-safe: two downloads racing the first render would corrupt the
    // embedded font subset. Serialize every render in this process.
    private static readonly SemaphoreSlim renderLock = new(1, 1);

    private readonly IPdfService pdfService;
    private readonly IBlobStorageService blobStorage;
    private readonly ILogger<ContractPdfService> logger;

    public ContractPdfService(
        IPdfService pdfService,
        IBlobStorageService blobStorage,
        ILogger<ContractPdfService> logger)
    {
        this.pdfService = pdfService;
        this.blobStorage = blobStorage;
        this.logger = logger;
    }

    public async Task<byte[]> GetOrCreateAsync(ContractEntity contract, CancellationToken ct = default)
    {
        var blobName = contract.PdfBlobName
            ?? throw new InvalidOperationException("Contract has no assigned PDF blob name");

        if (await blobStorage.ExistsAsync(blobName))
        {
            await using var stream = await blobStorage.DownloadAsync(blobName);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }

        /* First download renders + stores, then reuses the blob thereafter. The contract is a pure
           deterministic function of the immutable snapshot, so rendering on demand is byte-identical to
           rendering at accept — no reason to pre-generate off a tenant-less background thread. */
        await renderLock.WaitAsync(ct);
        byte[] bytes;
        try { bytes = pdfService.Render(new ContractDocument(contract, logger)); }
        finally { renderLock.Release(); }

        using var upload = new MemoryStream(bytes, writable: false);
        await blobStorage.UploadAsync(upload, blobName);
        return bytes;
    }
}
