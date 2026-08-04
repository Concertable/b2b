using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ContractService : IContractService
{
    private readonly IContractRepository repository;
    private readonly IContractPdfService contractPdfService;

    public ContractService(
        IContractRepository repository,
        IContractPdfService contractPdfService)
    {
        this.repository = repository;
        this.contractPdfService = contractPdfService;
    }

    public Task<Result<ContractDto, ContractError>> GetByApplicationIdAsync(int applicationId) =>
        repository.GetByApplicationIdAsync(applicationId)
            .ToOption()
            .OrFailure(() => ContractError.ApplicationNotFound(applicationId))
            .Map(contract => contract.ToDto());

    public async Task<Result<FileDownload, ContractError>> GetPdfByApplicationIdAsync(int applicationId)
    {
        return await repository.GetByApplicationIdAsync(applicationId)
            .ToOption()
            .OrFailure(() => ContractError.ApplicationNotFound(applicationId))
            .MapAsync(async contract =>
                contract.ToFileDownload(await contractPdfService.GetOrCreateAsync(contract)));
    }

    public async Task<Result<FileDownload, ContractError>> GetPdfByConcertIdAsync(int concertId)
    {
        return await repository.GetByConcertIdAsync(concertId)
            .ToOption()
            .OrFailure(() => ContractError.ConcertNotFound(concertId))
            .MapAsync(async contract =>
                contract.ToFileDownload(await contractPdfService.GetOrCreateAsync(contract)));
    }
}
