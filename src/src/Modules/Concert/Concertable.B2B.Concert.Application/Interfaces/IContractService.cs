using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Reads a booking contract for one of its two parties (the tenant filter answers 404 to anyone
/// else, matching how the application itself reads). Serves the metadata and the PDF.
/// </summary>
internal interface IContractService
{
    Task<Option<ContractDto>> GetByApplicationIdAsync(int applicationId);
    Task<Option<FileDownload>> GetPdfByApplicationIdAsync(int applicationId);
    Task<Option<FileDownload>> GetPdfByConcertIdAsync(int concertId);
}
