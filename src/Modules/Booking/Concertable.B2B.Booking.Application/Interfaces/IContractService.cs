using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Errors;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IContractService
{
    Task<Result<ContractDto, ContractError>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<Result<FileDownload, ContractError>> GetPdfByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<Result<FileDownload, ContractError>> GetPdfByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default);
}
