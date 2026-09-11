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
    private readonly IBlobStorageService blobStorageService;
    private readonly ILogger<ContractPdfRenderer> logger;

    public ContractPdfRenderer(
        IPdfRenderer pdfRenderer,
        IBlobStorageService blobStorageService,
        ILogger<ContractPdfRenderer> logger)
    {
        this.pdfRenderer = pdfRenderer;
        this.blobStorageService = blobStorageService;
        this.logger = logger;
    }

    public async Task<byte[]> GetOrCreateAsync(
        ContractEntity contract,
        CancellationToken ct = default)
    {
        var blobName = contract.PdfBlobName
            ?? throw new InvalidOperationException("Contract has no assigned PDF blob name");
        if (await blobStorageService.ExistsAsync(blobName))
        {
            await using var stream = await blobStorageService.DownloadAsync(blobName);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }

        var bytes = pdfRenderer.Render(new ContractDocument(contract, logger));
        using var upload = new MemoryStream(bytes, writable: false);
        await blobStorageService.UploadAsync(upload, blobName);
        return bytes;
    }
}
