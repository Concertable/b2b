using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;

namespace Concertable.B2B.Application.Application.Mappers;

internal sealed class ApplicationMapper : IApplicationMapper
{
    private readonly IArtistModule artists;
    private readonly IOpportunityModule opportunities;
    private readonly IVenueModule venues;
    private readonly IDealModule deals;

    public ApplicationMapper(
        IArtistModule artists,
        IOpportunityModule opportunities,
        IVenueModule venues,
        IDealModule deals)
    {
        this.artists = artists;
        this.opportunities = opportunities;
        this.venues = venues;
        this.deals = deals;
    }

    public async Task<ApplicationDto> ToDtoAsync(ApplicationEntity application) =>
        (await ToDtosAsync([application])).Single();

    public async Task<IReadOnlyList<ApplicationDto>> ToDtosAsync(IEnumerable<ApplicationEntity> applications)
    {
        var applicationList = applications.ToList();
        var artistsById = (await artists.GetSummariesAsync(
                applicationList.Select(application => application.ArtistId).Distinct().ToArray()))
            .ToDictionary(artist => artist.Id);
        var opportunitiesById = (await opportunities.GetDetailsAsync(
                applicationList.Select(application => application.OpportunityId).Distinct().ToArray()))
            .ToDictionary(opportunity => opportunity.OpportunityId);
        var dealsById = (await deals.GetByIdsAsync(
                opportunitiesById.Values.Select(opportunity => opportunity.DealId).Distinct()))
            .ToDictionary(deal => deal.Id);
        var venuesById = (await venues.GetProfilesAsync(
                opportunitiesById.Values.Select(opportunity => opportunity.VenueId).Distinct().ToArray()))
            .ToDictionary(venue => venue.Id);

        return applicationList.Select(application =>
        {
            if (!artistsById.TryGetValue(application.ArtistId, out var artist))
                throw new InvalidOperationException(
                    $"Artist {application.ArtistId} not found for application {application.Id}.");
            if (!opportunitiesById.TryGetValue(application.OpportunityId, out var opportunity))
                throw new InvalidOperationException(
                    $"Opportunity {application.OpportunityId} not found for application {application.Id}.");
            if (!dealsById.TryGetValue(opportunity.DealId, out var deal))
                throw new InvalidOperationException(
                    $"Deal {opportunity.DealId} not found for opportunity {opportunity.OpportunityId}.");
            if (!venuesById.TryGetValue(opportunity.VenueId, out var venue))
                throw new InvalidOperationException(
                    $"Venue {opportunity.VenueId} not found for opportunity {opportunity.OpportunityId}.");

            return new ApplicationDto(
                application.Id,
                artist,
                new OpportunitySnapshot(
                    opportunity.OpportunityId,
                    opportunity.VenueId,
                    venue.Name,
                    opportunity.StartDate,
                    opportunity.EndDate,
                    opportunity.Genres,
                    deal),
                application.State.ToStatus(),
                application.State);
        }).ToList();
    }
}
