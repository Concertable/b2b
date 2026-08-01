using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IInvoiceService
{
    Task<Option<InvoiceDto>> GetByConcertIdAsync(int concertId);
    Task<Option<FileDownload>> GetPdfByConcertIdAsync(int concertId);
}
