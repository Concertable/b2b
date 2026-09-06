using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;

namespace Concertable.B2B.Application.Application.Mappers;

internal sealed class ApplicationMapper : IApplicationMapper
{
    private readonly IArtistModule artistModule;
    private readonly IOpportunityModule opportunityModule;
    private readonly IVenueModule venueModule;
    private readonly IDealModule dealModule;

    public ApplicationMapper(
        IArtistModule artistModule,
        IOpportunityModule opportunityModule,
        IVenueModule venueModule,
        IDealModule dealModule)
    {
        this.artistModule = artistModule;
        this.opportunityModule = opportunityModule;
        this.venueModule = venueModule;
        this.dealModule = dealModule;
    }

    public async Task<ApplicationDto> ToDtoAsync(
        ApplicationEntity application,
        CancellationToken ct = default) =>
        (await ToDtosAsync([application], ct)).Single();

    public async Task<IReadOnlyList<ApplicationDto>> ToDtosAsync(
        IEnumerable<ApplicationEntity> applications,
        CancellationToken ct = default)
    {
        var applicationList = applications.ToList();
        var artistsById = (await artistModule.GetSummariesAsync(
                applicationList.Select(application => application.ArtistId).Distinct().ToArray(), ct))
            .ToDictionary(artist => artist.Id);
        var opportunitiesById = (await opportunityModule.GetAsync(
                applicationList.Select(application => application.OpportunityId).Distinct().ToArray(), ct))
            .ToDictionary(opportunity => opportunity.Id);
        var dealsById = (await dealModule.GetByIdsAsync(
                opportunitiesById.Values.Select(opportunity => opportunity.DealId).Distinct(), ct))
            .ToDictionary(deal => deal.Id);
        var venuesById = (await venueModule.GetProfilesAsync(
                opportunitiesById.Values.Select(opportunity => opportunity.VenueId).Distinct().ToArray(), ct))
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
                    $"Deal {opportunity.DealId} not found for opportunity {opportunity.Id}.");
            if (!venuesById.TryGetValue(opportunity.VenueId, out var venue))
                throw new InvalidOperationException(
                    $"Venue {opportunity.VenueId} not found for opportunity {opportunity.Id}.");

            return new ApplicationDto(
                application.Id,
                artist,
                new OpportunitySummary(
                    opportunity.Id,
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
