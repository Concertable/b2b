using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Errors;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Mappers;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class ContractService : IContractService
{
    private readonly IContractRepository repository;
    private readonly IContractPdfRenderer pdfRenderer;

    public ContractService(IContractRepository repository, IContractPdfRenderer pdfRenderer)
    {
        this.repository = repository;
        this.pdfRenderer = pdfRenderer;
    }

    public Task<Result<ContractDto, ContractError>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        repository.GetByApplicationIdAsync(applicationId, ct)
            .ToOption()
            .OrFailure(() => (ContractError)new ContractError.ApplicationNotFound(applicationId))
            .Map(contract => contract.ToDto());

    public async Task<Result<FileDownload, ContractError>> GetPdfByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await repository.GetByApplicationIdAsync(applicationId, ct)
            .ToOption()
            .OrFailure(() => (ContractError)new ContractError.ApplicationNotFound(applicationId))
            .MapAsync(async contract =>
                contract.ToFileDownload(await pdfRenderer.GetOrCreateAsync(contract, ct)));

    public async Task<Result<FileDownload, ContractError>> GetPdfByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default) =>
        await repository.GetByBookingIdAsync(bookingId, ct)
            .ToOption()
            .OrFailure(() => (ContractError)new ContractError.BookingNotFound(bookingId))
            .MapAsync(async contract =>
                contract.ToFileDownload(await pdfRenderer.GetOrCreateAsync(contract, ct)));
}
