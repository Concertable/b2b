using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts;

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
        var dealOption = await dealModule.GetByIdAsync(opportunity.DealId);
        if (!dealOption.TryGetValue(out var deal))
            throw new InvalidOperationException($"Opportunity {opportunity.Id} references missing deal {opportunity.DealId}.");

        return opportunity.ToDto(deal);
    }

    public async Task<IReadOnlyList<OpportunityDto>> ToDtosAsync(IEnumerable<OpportunityEntity> opportunities)
    {
        var opportunityList = opportunities.ToList();
        var dealMap = (await dealModule.GetByIdsAsync(opportunityList.Select(o => o.DealId).Distinct()))
            .ToDictionary(c => c.Id);

        return opportunityList.Select(o =>
        {
            if (!dealMap.TryGetValue(o.DealId, out var deal))
                throw new InvalidOperationException($"Opportunity {o.Id} references missing deal {o.DealId}.");
            return o.ToDto(deal);
        }).ToList();
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
        VenueName = opportunity.Venue.Name,
        DealId = opportunity.DealId,
        Deal = deal,
        StartDate = opportunity.Period.Start,
        EndDate = opportunity.Period.End,
        Genres = opportunity.Genres
    };
}
