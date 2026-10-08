using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;

namespace Concertable.B2B.Application.Application.Resolvers;

internal sealed class ApplicationResolver : IApplicationResolver
{
    private readonly IArtistModule artistModule;
    private readonly IOpportunityModule opportunityModule;
    private readonly IVenueModule venueModule;
    private readonly IDealModule dealModule;

    public ApplicationResolver(
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

    public async Task<ApplicationSummary> ResolveSummaryAsync(
        ApplicationEntity application,
        CancellationToken ct = default) =>
        (await ResolveSummariesAsync([application], ct)).Single();

    public async Task<IReadOnlyList<ApplicationSummary>> ResolveSummariesAsync(
        IEnumerable<ApplicationEntity> applications,
        CancellationToken ct = default)
    {
        var batch = await LoadBatchAsync(applications, ct);
        return batch.Applications.Select(application =>
        {
            var artist = batch.Artists.GetValueOrDefault(application.ArtistId)
                ?? throw new InvalidOperationException(
                    $"Artist {application.ArtistId} not found for application {application.Id}.");
            var opportunity = batch.Opportunities.GetValueOrDefault(application.OpportunityId)
                ?? throw new InvalidOperationException(
                    $"Opportunity {application.OpportunityId} not found for application {application.Id}.");
            var venue = batch.Venues.GetValueOrDefault(opportunity.VenueId)
                ?? throw new InvalidOperationException(
                    $"Venue {opportunity.VenueId} not found for opportunity {opportunity.Id}.");

            return application.ToSummary(artist, opportunity, venue);
        }).ToList();
    }

    public async Task<ApplicationProposal> ResolveProposalAsync(
        ApplicationEntity application,
        CancellationToken ct = default) =>
        (await ResolveProposalsAsync([application], ct)).Single();

    public async Task<IReadOnlyList<ApplicationProposal>> ResolveProposalsAsync(
        IEnumerable<ApplicationEntity> applications,
        CancellationToken ct = default)
    {
        var batch = await LoadBatchAsync(applications, ct);
        var deals = (await dealModule.GetByIdsAsync(
                batch.Opportunities.Values.Select(opportunity => opportunity.DealId).Distinct(), ct))
            .ToDictionary(deal => deal.Id);

        return batch.Applications.Select(application =>
        {
            var artist = batch.Artists.GetValueOrDefault(application.ArtistId)
                ?? throw new InvalidOperationException(
                    $"Artist {application.ArtistId} not found for application {application.Id}.");
            var opportunity = batch.Opportunities.GetValueOrDefault(application.OpportunityId)
                ?? throw new InvalidOperationException(
                    $"Opportunity {application.OpportunityId} not found for application {application.Id}.");
            var venue = batch.Venues.GetValueOrDefault(opportunity.VenueId)
                ?? throw new InvalidOperationException(
                    $"Venue {opportunity.VenueId} not found for opportunity {opportunity.Id}.");
            var deal = deals.GetValueOrDefault(opportunity.DealId)
                ?? throw new InvalidOperationException(
                    $"Deal {opportunity.DealId} not found for opportunity {opportunity.Id}.");

            return application.ToProposal(artist, opportunity, venue, deal);
        }).ToList();
    }

    private async Task<ApplicationBatch> LoadBatchAsync(
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
        return new ApplicationBatch(applicationList, artists, opportunities, venues);
    }

    private sealed record ApplicationBatch(
        IReadOnlyList<ApplicationEntity> Applications,
        IReadOnlyDictionary<int, ArtistSummary> Artists,
        IReadOnlyDictionary<int, OpportunityDto> Opportunities,
        IReadOnlyDictionary<int, VenueProfile> Venues);
}
