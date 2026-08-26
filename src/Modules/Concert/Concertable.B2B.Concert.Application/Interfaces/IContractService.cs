using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Reads a booking contract for one of its two parties (the tenant filter answers 404 to anyone
/// else, matching how the application itself reads). Serves the metadata and the PDF.
/// </summary>
internal interface IContractService
{
    Task<Result<ContractDto, ContractError>> GetByApplicationIdAsync(int applicationId);
    Task<Result<FileDownload, ContractError>> GetPdfByApplicationIdAsync(int applicationId);
    Task<Result<FileDownload, ContractError>> GetPdfByConcertIdAsync(int concertId);
}
