namespace Concertable.B2B.Venue.Infrastructure;

internal sealed class VenueModule : IVenueModule
{
    private readonly IVenueService venueService;

    public VenueModule(IVenueService venueService)
    {
        this.venueService = venueService;
    }

    public Task<Option<VenueSummary>> GetSummaryAsync(int venueId, CancellationToken ct = default) =>
        venueService.GetSummaryAsync(venueId);

    public Task<Option<int>> GetVenueIdForCurrentTenantAsync(CancellationToken ct = default) =>
        venueService.GetIdForCurrentTenantAsync();

    public Task<Option<VenueOrgIdentity>> GetOrgIdentityByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        venueService.GetOrgIdentityByTenantIdAsync(tenantId);
}
