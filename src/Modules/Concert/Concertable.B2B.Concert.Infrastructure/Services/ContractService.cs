using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ContractService : IContractService
{
    private readonly IContractRepository repository;
    private readonly IContractPdfService pdfService;

    public ContractService(
        IContractRepository repository,
        IContractPdfService pdfService)
    {
        this.repository = repository;
        this.pdfService = pdfService;
    }

    public async Task<ContractDto> GetByApplicationIdAsync(int applicationId)
    {
        var agreement = await repository.GetByApplicationIdAsync(applicationId)
            .OrNotFound();
        return agreement.ToDto();
    }

    public async Task<FileDownload> GetPdfByApplicationIdAsync(int applicationId)
    {
        var agreement = await repository.GetByApplicationIdAsync(applicationId)
            .OrNotFound();
        return agreement.ToFileDownload(await pdfService.GetOrCreateAsync(agreement));
    }

    public async Task<FileDownload> GetPdfByConcertIdAsync(int concertId)
    {
        var agreement = await repository.GetByConcertIdAsync(concertId)
            .OrNotFound();
        return agreement.ToFileDownload(await pdfService.GetOrCreateAsync(agreement));
    }
}
