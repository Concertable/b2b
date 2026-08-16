using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class OpportunityDashboardService : IOpportunityDashboardService
{
    private readonly IOpportunityRepository repository;
    private readonly IPublicOpportunityRepository publicRepository;
    private readonly IVenueModule venueModule;
    private readonly IArtistModule artistModule;
    private readonly IDealModule dealModule;
    private readonly TimeProvider timeProvider;

    public OpportunityDashboardService(
        IOpportunityRepository repository,
        IPublicOpportunityRepository publicRepository,
        IVenueModule venueModule,
        IArtistModule artistModule,
        IDealModule dealModule,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.publicRepository = publicRepository;
        this.venueModule = venueModule;
        this.artistModule = artistModule;
        this.dealModule = dealModule;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<OpportunityApplicationMetrics>, OpportunityError>>
        GetApplicationMetricsForCurrentVenueAsync()
    {
        var venue = await venueModule.GetVenueIdForCurrentTenantAsync();
        if (!venue.TryGetValue(out var venueId))
            return new OpportunityError.MissingVenue();

        var projections = await repository.GetOpenWithApplicationCountsByVenueIdAsync(venueId);
        var deals = await GetDealsAsync(projections.Select(projection => projection.DealId));
        var today = timeProvider.GetUtcNow().UtcDateTime.Date;

        return new Success<IReadOnlyList<OpportunityApplicationMetrics>>(
            projections.ToApplicationMetrics(deals, today));
    }

    public async Task<Result<IReadOnlyList<OpportunityMatch>, OpportunityError>> GetMatchesForCurrentArtistAsync()
    {
        var artist = await artistModule.GetIdForCurrentTenantAsync();
        if (!artist.TryGetValue(out var artistId))
            return new OpportunityError.MissingArtist();

        var artistGenres = await artistModule.GetGenresAsync(artistId);
        var projections = await publicRepository.GetMatchCandidatesAsync(artistId, artistGenres);
        var deals = await GetDealsAsync(projections.Select(projection => projection.DealId));

        return new Success<IReadOnlyList<OpportunityMatch>>(
            projections.ToMatches(deals, artistGenres));
    }

    private async Task<IReadOnlyDictionary<int, IDeal>> GetDealsAsync(IEnumerable<int> dealIds) =>
        (await dealModule.GetByIdsAsync(dealIds.Distinct()))
            .ToDictionary(deal => deal.Id);
}
