using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IInvoiceService
{
    Task<InvoiceDto> GetByConcertIdAsync(int concertId);
    Task<FileDownload> GetPdfByConcertIdAsync(int concertId);
}
