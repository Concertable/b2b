using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Reads a booking agreement for one of its two parties (the tenant filter answers 404 to anyone
/// else, matching how the application itself reads). Serves the metadata and the PDF.
/// </summary>
internal interface IBookingAgreementService
{
    Task<BookingAgreementDto> GetByApplicationIdAsync(int applicationId);
    Task<AgreementPdf> GetPdfByApplicationIdAsync(int applicationId);
    Task<AgreementPdf> GetPdfByConcertIdAsync(int concertId);
}
