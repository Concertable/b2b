using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class OpportunityMapper : IOpportunityMapper
{
    private readonly IDealModule dealModule;

    public OpportunityMapper(IDealModule dealModule)
    {
        this.dealModule = dealModule;
    }

    public async Task<OpportunityDto> ToDtoAsync(OpportunityEntity opportunity)
    {
        var deal = await dealModule.GetByIdAsync(opportunity.DealId)
            .OrNotFound($"Deal {opportunity.DealId}");
        return opportunity.ToDto(deal);
    }

    public async Task<IEnumerable<OpportunityDto>> ToDtosAsync(IEnumerable<OpportunityEntity> opportunities)
    {
        var opportunityList = opportunities.ToList();
        var dealMap = (await dealModule.GetByIdsAsync(opportunityList.Select(o => o.DealId).Distinct()))
            .ToDictionary(c => c.Id);

        return opportunityList.Select(o =>
        {
            if (!dealMap.TryGetValue(o.DealId, out var deal))
                throw new NotFoundException($"Deal {o.DealId} not found");
            return o.ToDto(deal);
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
    public static OpportunityDto ToDto(this OpportunityEntity opportunity, IDeal deal) => new()
    {
        Id = opportunity.Id,
        VenueId = opportunity.VenueId,
        DealId = opportunity.DealId,
        Deal = deal,
        StartDate = opportunity.Period.Start,
        EndDate = opportunity.Period.End,
        Genres = opportunity.Genres
    };
}
