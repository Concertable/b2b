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

    public async Task<ApplicationSummaryDto> ToSummaryAsync(
        ApplicationEntity application,
        CancellationToken ct = default) =>
        (await ToSummariesAsync([application], ct)).Single();

    public async Task<IReadOnlyList<ApplicationSummaryDto>> ToSummariesAsync(
        IEnumerable<ApplicationEntity> applications,
        CancellationToken ct = default)
    {
        var facts = await LoadFactsAsync(applications, ct);
        return facts.Applications.Select(application =>
        {
            var artist = facts.Artists.GetValueOrDefault(application.ArtistId)
                ?? throw new InvalidOperationException(
                    $"Artist {application.ArtistId} not found for application {application.Id}.");
            var opportunity = facts.Opportunities.GetValueOrDefault(application.OpportunityId)
                ?? throw new InvalidOperationException(
                    $"Opportunity {application.OpportunityId} not found for application {application.Id}.");
            var venue = facts.Venues.GetValueOrDefault(opportunity.VenueId)
                ?? throw new InvalidOperationException(
                    $"Venue {opportunity.VenueId} not found for opportunity {opportunity.Id}.");

            return new ApplicationSummaryDto(
                application.Id,
                application.VenueTenantId,
                application.ArtistTenantId,
                artist,
                new OpportunitySummary(
                    opportunity.Id,
                    opportunity.VenueId,
                    venue.Name,
                    opportunity.StartDate,
                    opportunity.EndDate,
                    opportunity.Genres),
                application.State.ToStatus(),
                application.State);
        }).ToList();
    }

    public async Task<ApplicationProposalDto> ToProposalAsync(
        ApplicationEntity application,
        CancellationToken ct = default) =>
        (await ToProposalsAsync([application], ct)).Single();

    public async Task<IReadOnlyList<ApplicationProposalDto>> ToProposalsAsync(
        IEnumerable<ApplicationEntity> applications,
        CancellationToken ct = default)
    {
        var facts = await LoadFactsAsync(applications, ct);
        var deals = (await dealModule.GetByIdsAsync(
                facts.Opportunities.Values.Select(opportunity => opportunity.DealId).Distinct(), ct))
            .ToDictionary(deal => deal.Id);

        return facts.Applications.Select(application =>
        {
            var artist = facts.Artists.GetValueOrDefault(application.ArtistId)
                ?? throw new InvalidOperationException(
                    $"Artist {application.ArtistId} not found for application {application.Id}.");
            var opportunity = facts.Opportunities.GetValueOrDefault(application.OpportunityId)
                ?? throw new InvalidOperationException(
                    $"Opportunity {application.OpportunityId} not found for application {application.Id}.");
            var venue = facts.Venues.GetValueOrDefault(opportunity.VenueId)
                ?? throw new InvalidOperationException(
                    $"Venue {opportunity.VenueId} not found for opportunity {opportunity.Id}.");
            var deal = deals.GetValueOrDefault(opportunity.DealId)
                ?? throw new InvalidOperationException(
                    $"Deal {opportunity.DealId} not found for opportunity {opportunity.Id}.");

            return new ApplicationProposalDto(
                application.Id,
                application.VenueTenantId,
                application.ArtistTenantId,
                artist,
                new OpportunityProposal(
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

    private async Task<ApplicationFacts> LoadFactsAsync(
        IEnumerable<ApplicationEntity> applications,
        CancellationToken ct)
    {
        var applicationList = applications.ToList();
        var artists = (await artistModule.GetSummariesAsync(
                applicationList.Select(application => application.ArtistId).Distinct().ToArray(), ct))
            .ToDictionary(artist => artist.Id);
        var opportunities = (await opportunityModule.GetAsync(
                applicationList.Select(application => application.OpportunityId).Distinct().ToArray(), ct))
            .ToDictionary(opportunity => opportunity.Id);
        var venues = (await venueModule.GetProfilesAsync(
                opportunities.Values.Select(opportunity => opportunity.VenueId).Distinct().ToArray(), ct))
            .ToDictionary(venue => venue.Id);
        return new ApplicationFacts(applicationList, artists, opportunities, venues);
    }

    private sealed record ApplicationFacts(
        IReadOnlyList<ApplicationEntity> Applications,
        IReadOnlyDictionary<int, ArtistSummary> Artists,
        IReadOnlyDictionary<int, OpportunityDto> Opportunities,
        IReadOnlyDictionary<int, VenueProfile> Venues);
}
