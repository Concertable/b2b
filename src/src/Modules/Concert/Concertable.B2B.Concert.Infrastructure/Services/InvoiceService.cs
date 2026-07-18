using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository repository;
    private readonly IInvoicePdfService pdfService;

    public InvoiceService(IInvoiceRepository repository, IInvoicePdfService pdfService)
    {
        this.repository = repository;
        this.pdfService = pdfService;
    }

    public async Task<InvoiceDto> GetByConcertIdAsync(int concertId)
    {
        var invoice = await repository.GetByConcertIdAsync(concertId)
            .OrNotFound();
        return invoice.ToDto();
    }

    public async Task<FileDownload> GetPdfByConcertIdAsync(int concertId)
    {
        var invoice = await repository.GetByConcertIdAsync(concertId)
            .OrNotFound();
        return invoice.ToFileDownload(await pdfService.GetOrCreateAsync(invoice));
    }
}
