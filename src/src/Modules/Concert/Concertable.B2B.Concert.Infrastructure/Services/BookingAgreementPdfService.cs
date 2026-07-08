using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Pdf;
using Concertable.Shared.Blob.Application;
using Concertable.Shared.Pdf.Application;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class BookingAgreementPdfService : IBookingAgreementPdfService
{
    private readonly IPdfService pdfService;
    private readonly IBlobStorageService blobStorage;
    private readonly IBookingAgreementRepository repository;

    public BookingAgreementPdfService(
        IPdfService pdfService,
        IBlobStorageService blobStorage,
        IBookingAgreementRepository repository)
    {
        this.pdfService = pdfService;
        this.blobStorage = blobStorage;
        this.repository = repository;
    }

    public async Task<byte[]> GetOrCreateAsync(BookingAgreementEntity agreement, CancellationToken ct = default)
    {
        if (agreement.PdfBlobName is not null && await blobStorage.ExistsAsync(agreement.PdfBlobName))
        {
            await using var stream = await blobStorage.DownloadAsync(agreement.PdfBlobName);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }

        return await RenderStoreAsync(agreement, ct);
    }

    public async Task GenerateForBookingAsync(int bookingId, CancellationToken ct = default)
    {
        var agreement = await repository.GetByBookingIdIgnoringTenantAsync(bookingId, ct);
        if (agreement is null || agreement.PdfBlobName is not null)
            return;

        await RenderStoreAsync(agreement, ct);
    }

    private async Task<byte[]> RenderStoreAsync(BookingAgreementEntity agreement, CancellationToken ct)
    {
        var bytes = pdfService.Render(new BookingAgreementDocument(agreement));
        var blobName = agreement.PdfBlobName ?? $"agreements/{agreement.BookingId}-{Guid.NewGuid():N}.pdf";

        using (var upload = new MemoryStream(bytes, writable: false))
            await blobStorage.UploadAsync(upload, blobName);

        if (agreement.PdfBlobName is null)
        {
            agreement.AttachPdf(blobName);
            await repository.SaveChangesAsync(ct);
        }

        return bytes;
    }
}
