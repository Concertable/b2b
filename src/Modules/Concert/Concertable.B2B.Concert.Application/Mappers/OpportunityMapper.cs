using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Contract.Contracts;
using Concertable.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class OpportunityMapper : IOpportunityMapper
{
    private readonly IContractModule contractModule;

    public OpportunityMapper(IContractModule contractModule)
    {
        this.contractModule = contractModule;
    }

    public async Task<OpportunityDto> ToDtoAsync(OpportunityEntity opportunity)
    {
        var contract = await contractModule.GetByIdAsync(opportunity.ContractId)
            ?? throw new NotFoundException($"Contract {opportunity.ContractId} not found");
        return opportunity.ToDto(contract);
    }

    public async Task<IEnumerable<OpportunityDto>> ToDtosAsync(IEnumerable<OpportunityEntity> opportunities)
    {
        var opportunityList = opportunities.ToList();
        var contractMap = (await contractModule.GetByIdsAsync(opportunityList.Select(o => o.ContractId).Distinct()))
            .ToDictionary(c => c.Id);

        return opportunityList.Select(o =>
        {
            if (!contractMap.TryGetValue(o.ContractId, out var contract))
                throw new NotFoundException($"Contract {o.ContractId} not found");
            return o.ToDto(contract);
        });
    }

    public async Task<IPagination<OpportunityDto>> ToDtosAsync(IPagination<OpportunityEntity> opportunities)
    {
        var dtos = await ToDtosAsync(opportunities.Data);
        return new Pagination<OpportunityDto>(dtos.ToList(), opportunities.TotalCount, opportunities.PageNumber, opportunities.PageSize);
    }
}

internal static class OpportunityMappers
{
    public static OpportunityDto ToDto(this OpportunityEntity opportunity, IContract contract) => new()
    {
        Id = opportunity.Id,
        VenueId = opportunity.VenueId,
        ContractId = opportunity.ContractId,
        Contract = contract,
        StartDate = opportunity.Period.Start,
        EndDate = opportunity.Period.End,
        Genres = opportunity.Genres
    };
}
