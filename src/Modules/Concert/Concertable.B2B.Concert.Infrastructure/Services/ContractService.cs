using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Exceptions;

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

    public async Task<ContractDto> GetByApplicationIdAsync(int applicationId)
    {
        var contract = await repository.GetByApplicationIdAsync(applicationId)
            .OrNotFound();
        return contract.ToDto();
    }

    public async Task<FileDownload> GetPdfByApplicationIdAsync(int applicationId)
    {
        var contract = await repository.GetByApplicationIdAsync(applicationId)
            .OrNotFound();
        return contract.ToFileDownload(await contractPdfService.GetOrCreateAsync(contract));
    }

    public async Task<FileDownload> GetPdfByConcertIdAsync(int concertId)
    {
        var contract = await repository.GetByConcertIdAsync(concertId)
            .OrNotFound();
        return contract.ToFileDownload(await contractPdfService.GetOrCreateAsync(contract));
    }
}
