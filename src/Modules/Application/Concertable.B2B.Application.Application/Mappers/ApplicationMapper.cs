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

    public async Task<ApplicationDto> ToDtoAsync(ApplicationEntity application)
    {
        var artistOption = await artists.GetSummaryAsync(application.ArtistId);
        if (!artistOption.TryGetValue(out var artist))
            throw new InvalidOperationException(
                $"Artist {application.ArtistId} not found for application {application.Id}.");

        var opportunityOption = await opportunities.GetDetailsAsync(application.OpportunityId);
        if (!opportunityOption.TryGetValue(out var opportunity))
            throw new InvalidOperationException(
                $"Opportunity {application.OpportunityId} not found for application {application.Id}.");

        var dealOption = await deals.GetByIdAsync(opportunity.DealId);
        if (!dealOption.TryGetValue(out var deal))
            throw new InvalidOperationException(
                $"Deal {opportunity.DealId} not found for opportunity {opportunity.OpportunityId}.");

        var venueOption = await venues.GetProfileAsync(opportunity.VenueId);
        if (!venueOption.TryGetValue(out var venue))
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
    }

    public async Task<IReadOnlyList<ApplicationDto>> ToDtosAsync(IEnumerable<ApplicationEntity> applications) =>
        await Task.WhenAll(applications.Select(ToDtoAsync));
}
