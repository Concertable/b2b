using Concertable.B2B.Concert.Application.DTOs;
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

    public async Task<Option<ContractDto>> GetByApplicationIdAsync(int applicationId) =>
        (await repository.GetByApplicationIdAsync(applicationId))
            .ToOption()
            .Map(contract => contract.ToDto());

    public async Task<Option<FileDownload>> GetPdfByApplicationIdAsync(int applicationId)
    {
        var contract = (await repository.GetByApplicationIdAsync(applicationId)).ToOption();
        return await contract.MapAsync(async value =>
            value.ToFileDownload(await contractPdfService.GetOrCreateAsync(value)));
    }

    public async Task<Option<FileDownload>> GetPdfByConcertIdAsync(int concertId)
    {
        var contract = (await repository.GetByConcertIdAsync(concertId)).ToOption();
        return await contract.MapAsync(async value =>
            value.ToFileDownload(await contractPdfService.GetOrCreateAsync(value)));
    }
}
