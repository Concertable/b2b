using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IInvoiceService
{
    Task<Result<InvoiceDto, InvoiceError>> GetByConcertIdAsync(int concertId);
    Task<Result<FileDownload, InvoiceError>> GetPdfByConcertIdAsync(int concertId);
}
