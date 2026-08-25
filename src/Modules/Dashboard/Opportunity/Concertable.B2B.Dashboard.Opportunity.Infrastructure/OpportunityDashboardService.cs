using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Dashboard.Opportunity.Application;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Contracts.Enums;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Reunion;

namespace Concertable.B2B.Dashboard.Opportunity.Infrastructure;

internal sealed class OpportunityDashboardService : IOpportunityDashboardService
{
    private readonly IApplicationModule applications;
    private readonly IArtistModule artists;
    private readonly IDealModule deals;
    private readonly IOpportunityModule opportunities;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;
    private readonly IVenueModule venues;

    public OpportunityDashboardService(
        IApplicationModule applications,
        IArtistModule artists,
        IDealModule deals,
        IOpportunityModule opportunities,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        IVenueModule venues)
    {
        this.applications = applications;
        this.artists = artists;
        this.deals = deals;
        this.opportunities = opportunities;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
        this.venues = venues;
    }

    public async Task<Result<IReadOnlyList<OpportunityApplicationMetrics>, OpportunityDashboardError>>
        GetOpenAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new OpportunityDashboardError.MissingVenue();

        var items = await opportunities.GetOpenByVenueTenantIdAsync(tenantId, ct);
        var counts = await applications.GetCountsByOpportunityIdsAsync(
            items.Select(item => item.Id).ToArray(),
            ct);
        var (dealsById, venuesById) = await GetLookupsAsync(items, ct);
        var today = timeProvider.GetUtcNow().UtcDateTime.Date;

        return items.Select(item => new OpportunityApplicationMetrics(
                item.ToSummary(dealsById, venuesById),
                counts.GetValueOrDefault(item.Id),
                OpportunityDashboardCalculator.CalculateDaysUntilDeadline(item.StartDate, today)))
            .ToList();
    }

    public async Task<Result<IReadOnlyList<OpportunityMatch>, OpportunityDashboardError>>
        GetRecommendedAsync(CancellationToken ct = default)
    {
        var artistOption = await artists.GetCurrentProfileAsync(ct);
        if (!artistOption.TryGetValue(out var artist))
            return new OpportunityDashboardError.MissingArtist();

        var excludedOpportunityIds = await applications.GetOpportunityIdsForArtistTenantAsync(
            artist.TenantId,
            ct);
        var items = await opportunities.GetRecommendedAsync(
            excludedOpportunityIds,
            artist.Genres,
            ct);
        var (dealsById, venuesById) = await GetLookupsAsync(items, ct);

        return items.Select(item => item.ToMatch(
                dealsById,
                venuesById,
                OpportunityDashboardCalculator.CalculateFitScore(item.Genres, artist.Genres)))
            .ToList();
    }

    private async Task<(
        IReadOnlyDictionary<int, DealDto> DealsById,
        IReadOnlyDictionary<int, VenueProfile> VenuesById)> GetLookupsAsync(
        IReadOnlyCollection<OpportunityDto> items,
        CancellationToken ct)
    {
        var dealIds = items.Select(item => item.DealId).Distinct().ToArray();
        var venueIds = items.Select(item => item.VenueId).Distinct().ToArray();
        var dealsTask = deals.GetByIdsAsync(dealIds, ct);
        var venuesTask = venues.GetProfilesAsync(venueIds, ct);
        await Task.WhenAll(dealsTask, venuesTask);

        var dealsById = (await dealsTask).ToDictionary(deal => deal.Id);
        var venuesById = (await venuesTask).ToDictionary(venue => venue.Id);
        foreach (var item in items)
        {
            if (!dealsById.ContainsKey(item.DealId))
                throw new InvalidOperationException($"Deal {item.DealId} was not found.");
            if (!venuesById.ContainsKey(item.VenueId))
                throw new InvalidOperationException($"Venue {item.VenueId} was not found.");
        }

        return (dealsById, venuesById);
    }

}
