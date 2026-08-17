using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Infrastructure.Pdf;
using Concertable.Shared.Blob.Application;
using Concertable.Shared.Pdf.Application;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class ContractPdfRenderer : IContractPdfRenderer
{
    private readonly IPdfRenderer pdfRenderer;
    private readonly IBlobStorageService blobStorage;
    private readonly ILogger<ContractPdfRenderer> logger;

    public ContractPdfRenderer(
        IPdfRenderer pdfRenderer,
        IBlobStorageService blobStorage,
        ILogger<ContractPdfRenderer> logger)
    {
        this.pdfRenderer = pdfRenderer;
        this.blobStorage = blobStorage;
        this.logger = logger;
    }

    public async Task<byte[]> GetOrCreateAsync(
        ContractEntity contract,
        CancellationToken ct = default)
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

        var bytes = pdfRenderer.Render(new ContractDocument(contract, logger));
        using var upload = new MemoryStream(bytes, writable: false);
        await blobStorage.UploadAsync(upload, blobName);
        return bytes;
    }
}
