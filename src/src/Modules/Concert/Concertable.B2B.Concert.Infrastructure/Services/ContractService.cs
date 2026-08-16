using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ContractService : IContractService
{
    private readonly IContractRepository repository;
    private readonly IContractPdfRenderer contractPdfRenderer;

    public ContractService(
        IContractRepository repository,
        IContractPdfRenderer contractPdfRenderer)
    {
        this.repository = repository;
        this.contractPdfRenderer = contractPdfRenderer;
    }

    public Task<Result<ContractDto, ContractError>> GetByApplicationIdAsync(int applicationId) =>
        repository.GetByApplicationIdAsync(applicationId)
            .ToOption()
            .OrFailure(() => (ContractError)new ContractError.ApplicationNotFound(applicationId))
            .Map(contract => contract.ToDto());

    public async Task<Result<FileDownload, ContractError>> GetPdfByApplicationIdAsync(int applicationId)
    {
        return await repository.GetByApplicationIdAsync(applicationId)
            .ToOption()
            .OrFailure(() => (ContractError)new ContractError.ApplicationNotFound(applicationId))
            .MapAsync(async contract =>
                contract.ToFileDownload(await contractPdfRenderer.GetOrCreateAsync(contract)));
    }

    public async Task<Result<FileDownload, ContractError>> GetPdfByConcertIdAsync(int concertId)
    {
        return await repository.GetByConcertIdAsync(concertId)
            .ToOption()
            .OrFailure(() => (ContractError)new ContractError.ConcertNotFound(concertId))
            .MapAsync(async contract =>
                contract.ToFileDownload(await contractPdfRenderer.GetOrCreateAsync(contract)));
    }
}
