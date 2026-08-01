using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Mappers;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository repository;
    private readonly IInvoicePdfService invoicePdfService;

    public InvoiceService(IInvoiceRepository repository, IInvoicePdfService invoicePdfService)
    {
        this.repository = repository;
        this.invoicePdfService = invoicePdfService;
    }

    public async Task<Option<InvoiceDto>> GetByConcertIdAsync(int concertId) =>
        (await repository.GetByConcertIdAsync(concertId))
            .ToOption()
            .Map(invoice => invoice.ToDto());

    public async Task<Option<FileDownload>> GetPdfByConcertIdAsync(int concertId)
    {
        var invoice = (await repository.GetByConcertIdAsync(concertId)).ToOption();
        return await invoice.MapAsync(async value =>
            value.ToFileDownload(await invoicePdfService.GetOrCreateAsync(value)));
    }
}
