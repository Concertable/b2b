using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository repository;

    public InvoiceService(IInvoiceRepository repository)
    {
        this.repository = repository;
    }

    public async Task<InvoiceDto> GetByConcertIdAsync(int concertId)
    {
        var invoice = await repository.GetByConcertIdAsync(concertId)
            .OrNotFound();
        return invoice.ToDto();
    }
}
