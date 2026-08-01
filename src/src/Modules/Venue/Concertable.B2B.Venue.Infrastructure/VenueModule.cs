namespace Concertable.B2B.Venue.Infrastructure;

internal sealed class VenueModule : IVenueModule
{
    private readonly IVenueService venueService;
    private readonly IVenueRepository repository;

    public VenueModule(IVenueService venueService, IVenueRepository repository)
    {
        this.venueService = venueService;
        this.repository = repository;
    }

    public Task<Option<VenueSummary>> GetSummaryAsync(int venueId, CancellationToken ct = default) =>
        venueService.GetSummaryAsync(venueId);

    public async Task<Option<int>> GetVenueIdForCurrentTenantAsync(CancellationToken ct = default) =>
        (await repository.GetIdForCurrentTenantAsync()).ToOption();

    public Task<Option<VenueOrgIdentity>> GetOrgIdentityByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        venueService.GetOrgIdentityByTenantIdAsync(tenantId);
}
