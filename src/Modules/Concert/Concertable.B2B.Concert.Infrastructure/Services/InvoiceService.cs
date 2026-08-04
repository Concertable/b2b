using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;
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

    public Task<Result<InvoiceDto, InvoiceError>> GetByConcertIdAsync(int concertId) =>
        repository.GetByConcertIdAsync(concertId)
            .ToOption()
            .OrFailure(() => InvoiceError.ConcertNotFound(concertId))
            .Map(invoice => invoice.ToDto());

    public async Task<Result<FileDownload, InvoiceError>> GetPdfByConcertIdAsync(int concertId)
    {
        return await repository.GetByConcertIdAsync(concertId)
            .ToOption()
            .OrFailure(() => InvoiceError.ConcertNotFound(concertId))
            .MapAsync(async invoice =>
                invoice.ToFileDownload(await invoicePdfService.GetOrCreateAsync(invoice)));
    }
}
