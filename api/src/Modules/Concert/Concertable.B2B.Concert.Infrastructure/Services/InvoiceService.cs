using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Repositories;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class InvoiceService(
    IInvoiceRepository repository,
    IInvoicePdfRenderer invoicePdfRenderer,
    IAuthorizationContext membership) : IInvoiceService
{
    public async Task<Result<InvoiceDto, InvoiceError>> GetByConcertIdAsync(int concertId) =>
        (await GetReadableAsync(concertId)).Map(invoice => invoice.ToDto());

    public async Task<Result<FileDownload, InvoiceError>> GetPdfByConcertIdAsync(int concertId) =>
        await (await GetReadableAsync(concertId)).MapAsync(async invoice =>
            invoice.ToFileDownload(await invoicePdfRenderer.GetOrCreateAsync(invoice)));

    private async Task<Result<InvoiceEntity, InvoiceError>> GetReadableAsync(int concertId)
    {
        if (concertId <= 0 || membership.Membership is null)
            return new InvoiceError.ConcertNotFound(concertId);

        var invoice = await repository.GetByConcertIdAsync(concertId);
        if (invoice is null)
            return new InvoiceError.ConcertNotFound(concertId);

        return invoice;
    }
}
