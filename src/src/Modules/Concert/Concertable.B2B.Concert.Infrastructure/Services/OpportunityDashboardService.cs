using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class OpportunityDashboardService : IOpportunityDashboardService
{
    private readonly IOpportunityRepository repository;
    private readonly IOpportunityReadRepository readRepository;
    private readonly IArtistReadModelRepository artistRepository;
    private readonly ITenantContext tenantContext;
    private readonly IDealModule dealModule;
    private readonly TimeProvider timeProvider;

    public OpportunityDashboardService(
        IOpportunityRepository repository,
        IOpportunityReadRepository readRepository,
        IArtistReadModelRepository artistRepository,
        ITenantContext tenantContext,
        IDealModule dealModule,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.readRepository = readRepository;
        this.artistRepository = artistRepository;
        this.tenantContext = tenantContext;
        this.dealModule = dealModule;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<OpportunityApplicationMetrics>, OpportunityError>>
        GetApplicationMetricsForCurrentVenueAsync()
    {
        var projections = await repository.GetOpenWithApplicationCountsByVenueTenantIdAsync(
            tenantContext.GetTenantId());
        var deals = await GetDealsAsync(projections.Select(projection => projection.DealId));
        var today = timeProvider.GetUtcNow().UtcDateTime.Date;

        return new Success<IReadOnlyList<OpportunityApplicationMetrics>>(
            projections.ToApplicationMetrics(deals, today));
    }

    public async Task<Result<IReadOnlyList<OpportunityMatch>, OpportunityError>> GetMatchesForCurrentArtistAsync()
    {
        var artist = await artistRepository.GetByTenantIdAsync(tenantContext.GetTenantId());
        if (artist is null)
            return new OpportunityError.MissingArtist();

        var artistGenres = artist.Genres.Select(value => value.Genre).ToHashSet();
        var projections = await readRepository.GetMatchCandidatesAsync(artist.Id, artistGenres);
        var deals = await GetDealsAsync(projections.Select(projection => projection.DealId));

        return new Success<IReadOnlyList<OpportunityMatch>>(
            projections.ToMatches(deals, artistGenres));
    }

    private async Task<IReadOnlyDictionary<int, IDeal>> GetDealsAsync(IEnumerable<int> dealIds) =>
        (await dealModule.GetByIdsAsync(dealIds.Distinct()))
            .ToDictionary(deal => deal.Id);
}
