using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Application.DTOs;
using Concertable.B2B.Venue.Contracts;
using Concertable.Contracts.Enums;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Opportunity.Infrastructure.Services;

internal sealed class OpportunityDashboardService : IOpportunityDashboardService
{
    private readonly IOpportunityRepository repository;
    private readonly IOpportunityReadRepository readRepository;
    private readonly IApplicationDashboardMetricsProvider applicationMetrics;
    private readonly IArtistModule artists;
    private readonly IVenueModule venues;
    private readonly IOpportunityMapper mapper;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;

    public OpportunityDashboardService(
        IOpportunityRepository repository,
        IOpportunityReadRepository readRepository,
        IApplicationDashboardMetricsProvider applicationMetrics,
        IArtistModule artists,
        IVenueModule venues,
        IOpportunityMapper mapper,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.readRepository = readRepository;
        this.applicationMetrics = applicationMetrics;
        this.artists = artists;
        this.venues = venues;
        this.mapper = mapper;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
    }

    public Task<IReadOnlySet<int>> GetUpcomingIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        readRepository.GetUpcomingIdsAsync(opportunityIds, ct);

    public Task<int> GetOpenCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        readRepository.GetOpenCountAsync(venueTenantId, ct);

    public async Task<Result<IReadOnlyList<OpportunityApplicationMetrics>, OpportunityError>>
        GetApplicationMetricsForCurrentVenueAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new OpportunityError.MissingVenue();

        var opportunities = await repository.GetOpenByVenueTenantIdAsync(tenantId, ct);
        var counts = await applicationMetrics.GetCountsByOpportunityIdsAsync(
            opportunities.Select(opportunity => opportunity.Id).ToArray(),
            ct);
        var dtos = await mapper.ToDtosAsync(opportunities);
        var today = timeProvider.GetUtcNow().UtcDateTime.Date;

        return new Success<IReadOnlyList<OpportunityApplicationMetrics>>(
            dtos.Select(opportunity => new OpportunityApplicationMetrics(
                    opportunity,
                    counts.GetValueOrDefault(opportunity.Id),
                    Math.Max(0, (opportunity.StartDate.Date.AddDays(-7) - today).Days)))
                .ToList());
    }

    public async Task<Result<IReadOnlyList<OpportunityMatch>, OpportunityError>>
        GetMatchesForCurrentArtistAsync(CancellationToken ct = default)
    {
        var artistOption = await artists.GetCurrentProfileAsync(ct);
        if (!artistOption.TryGetValue(out var artist))
            return new OpportunityError.MissingArtist();

        var excludedOpportunityIds = await applicationMetrics.GetOpportunityIdsForArtistTenantAsync(
            artist.TenantId,
            ct);
        var opportunities = await readRepository.GetMatchCandidatesAsync(
            excludedOpportunityIds,
            artist.Genres,
            ct);
        var dtos = await mapper.ToDtosAsync(opportunities);
        var venueProfiles = await GetVenueProfilesAsync(dtos, ct);

        return new Success<IReadOnlyList<OpportunityMatch>>(
            dtos.Select(opportunity => new OpportunityMatch(
                    opportunity,
                    venueProfiles[opportunity.VenueId].County,
                    venueProfiles[opportunity.VenueId].Town,
                    CalculateFitScore(opportunity.Genres, artist.Genres)))
                .ToList());
    }

    private async Task<IReadOnlyDictionary<int, VenueProfile>> GetVenueProfilesAsync(
        IReadOnlyCollection<OpportunityDto> opportunities,
        CancellationToken ct)
    {
        var venueIds = opportunities.Select(opportunity => opportunity.VenueId).Distinct().ToArray();
        var profiles = await venues.GetProfilesAsync(venueIds, ct);
        var missingVenueId = venueIds.FirstOrDefault(venueId => profiles.All(profile => profile.Id != venueId));
        if (missingVenueId != 0)
            throw new InvalidOperationException($"Venue {missingVenueId} was not found.");
        return profiles.ToDictionary(profile => profile.Id);
    }

    private static int CalculateFitScore(
        IReadOnlyList<Genre> opportunityGenres,
        IReadOnlySet<Genre> artistGenres)
    {
        if (opportunityGenres.Count == 0)
            return 100;

        var matchingGenres = opportunityGenres.Count(artistGenres.Contains);
        return (int)Math.Round(matchingGenres * 100d / opportunityGenres.Count);
    }
}
