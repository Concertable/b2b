using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Opportunity.Application.DTOs;
using Concertable.B2B.Opportunity.Application.Interfaces;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.B2B.Venue.Contracts;
using Concertable.Contracts;

namespace Concertable.B2B.Opportunity.Application.Mappers;

internal sealed class OpportunityMapper : IOpportunityMapper
{
    private readonly IDealModule deals;
    private readonly IVenueModule venues;

    public OpportunityMapper(IDealModule deals, IVenueModule venues)
    {
        this.deals = deals;
        this.venues = venues;
    }

    public async Task<OpportunityDto> ToDtoAsync(OpportunityEntity opportunity)
    {
        var dealOption = await deals.GetByIdAsync(opportunity.DealId);
        if (!dealOption.TryGetValue(out var deal))
            throw new InvalidOperationException(
                $"Opportunity {opportunity.Id} references missing deal {opportunity.DealId}.");

        var venueOption = await venues.GetProfileAsync(opportunity.VenueId);
        if (!venueOption.TryGetValue(out var venue))
            throw new InvalidOperationException(
                $"Opportunity {opportunity.Id} references missing venue {opportunity.VenueId}.");

        return opportunity.ToDto(deal, venue.Name);
    }

    public async Task<IReadOnlyList<OpportunityDto>> ToDtosAsync(IEnumerable<OpportunityEntity> opportunities)
    {
        var opportunityList = opportunities.ToList();
        var dealsById = await GetDealsByIdAsync(opportunityList);
        var venueNamesById = await GetVenueNamesByIdAsync(opportunityList);
        return opportunityList
            .Select(opportunity => ToDto(opportunity, dealsById, venueNamesById))
            .ToList();
    }

    public async Task<IPagination<OpportunityDto>> ToDtosAsync(IPagination<OpportunityEntity> opportunities)
    {
        var dealsById = await GetDealsByIdAsync(opportunities.Data);
        var venueNamesById = await GetVenueNamesByIdAsync(opportunities.Data);
        return opportunities.Map(opportunity => ToDto(opportunity, dealsById, venueNamesById));
    }

    private async Task<IReadOnlyDictionary<int, DealDto>> GetDealsByIdAsync(
        IReadOnlyCollection<OpportunityEntity> opportunities) =>
        (await deals.GetByIdsAsync(opportunities.Select(opportunity => opportunity.DealId).Distinct()))
            .ToDictionary(deal => deal.Id);

    private async Task<IReadOnlyDictionary<int, string>> GetVenueNamesByIdAsync(
        IReadOnlyCollection<OpportunityEntity> opportunities)
    {
        var venueIds = opportunities.Select(opportunity => opportunity.VenueId).Distinct().ToArray();
        var profiles = await Task.WhenAll(venueIds.Select(async venueId =>
        {
            var profileOption = await venues.GetProfileAsync(venueId);
            if (!profileOption.TryGetValue(out var profile))
                throw new InvalidOperationException($"Venue {venueId} was not found.");
            return profile;
        }));
        return profiles.ToDictionary(profile => profile.Id, profile => profile.Name);
    }

    private static OpportunityDto ToDto(
        OpportunityEntity opportunity,
        IReadOnlyDictionary<int, DealDto> dealsById,
        IReadOnlyDictionary<int, string> venueNamesById) =>
        dealsById.TryGetValue(opportunity.DealId, out var deal) &&
        venueNamesById.TryGetValue(opportunity.VenueId, out var venueName)
            ? opportunity.ToDto(deal, venueName)
            : throw new InvalidOperationException($"Opportunity {opportunity.Id} has unresolved dependencies.");
}

internal static class OpportunityMappers
{
    extension(OpportunityEntity opportunity)
    {
        public OpportunityDto ToDto(DealDto deal, string venueName) => new()
        {
            Id = opportunity.Id,
            VenueId = opportunity.VenueId,
            VenueName = venueName,
            DealId = opportunity.DealId,
            Deal = deal,
            StartDate = opportunity.Period.Start,
            EndDate = opportunity.Period.End,
            Genres = opportunity.Genres
        };
    }
}
