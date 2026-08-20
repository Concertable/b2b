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
        var deals = await DealsByIdAsync(opportunityList);

        return opportunityList.Select(o => ToDto(o, deals)).ToList();
    }

    public async Task<IPagination<OpportunityDto>> ToDtosAsync(IPagination<OpportunityEntity> opportunities)
    {
        var deals = await DealsByIdAsync(opportunities.Data);

        return opportunities.Map(o => ToDto(o, deals));
    }

    private async Task<Dictionary<int, IDeal>> DealsByIdAsync(IReadOnlyCollection<OpportunityEntity> opportunities) =>
        (await dealModule.GetByIdsAsync(opportunities.Select(o => o.DealId).Distinct()))
            .ToDictionary(deal => deal.Id);

    private static OpportunityDto ToDto(OpportunityEntity opportunity, Dictionary<int, IDeal> deals) =>
        deals.TryGetValue(opportunity.DealId, out var deal)
            ? opportunity.ToDto(deal)
            : throw new InvalidOperationException($"Opportunity {opportunity.Id} references missing deal {opportunity.DealId}.");
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
