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
    private readonly IApplicationModule applicationModule;
    private readonly IArtistModule artistModule;
    private readonly IDealModule dealModule;
    private readonly IOpportunityModule opportunityModule;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;
    private readonly IVenueModule venueModule;

    public OpportunityDashboardService(
        IApplicationModule applicationModule,
        IArtistModule artistModule,
        IDealModule dealModule,
        IOpportunityModule opportunityModule,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        IVenueModule venueModule)
    {
        this.applicationModule = applicationModule;
        this.artistModule = artistModule;
        this.dealModule = dealModule;
        this.opportunityModule = opportunityModule;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
        this.venueModule = venueModule;
    }

    public async Task<Result<IReadOnlyList<OpportunityApplicationMetrics>, OpportunityDashboardError>>
        GetOpenAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new OpportunityDashboardError.MissingVenue();

        var items = await opportunityModule.GetOpenByVenueTenantIdAsync(tenantId, ct);
        var counts = await applicationModule.GetCountsByOpportunityIdsAsync(
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
        var artistOption = await artistModule.GetCurrentProfileAsync(ct);
        if (!artistOption.TryGetValue(out var artist))
            return new OpportunityDashboardError.MissingArtist();

        var excludedOpportunityIds = await applicationModule.GetOpportunityIdsForArtistTenantAsync(
            artist.TenantId,
            ct);
        var items = await opportunityModule.GetRecommendedAsync(
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
        IReadOnlyDictionary<int, VenueDto> VenuesById)> GetLookupsAsync(
        IReadOnlyCollection<OpportunityDto> items,
        CancellationToken ct)
    {
        var dealIds = items.Select(item => item.DealId).Distinct().ToArray();
        var venueIds = items.Select(item => item.VenueId).Distinct().ToArray();
        var dealsTask = dealModule.GetByIdsAsync(dealIds, ct);
        var venuesTask = venueModule.GetProfilesAsync(venueIds, ct);
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
