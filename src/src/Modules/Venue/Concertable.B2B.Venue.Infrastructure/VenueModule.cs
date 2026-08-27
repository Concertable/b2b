namespace Concertable.B2B.Venue.Infrastructure;

internal sealed class VenueModule : IVenueModule
{
    private readonly IVenueService venueService;

    public VenueModule(IVenueService venueService)
    {
        this.venueService = venueService;
    }

    public Task<Option<VenueSummary>> GetSummaryAsync(int venueId, CancellationToken ct = default) =>
        venueService.GetSummaryAsync(venueId, ct);

    public Task<Option<TenantContact>> GetContactByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        venueService.GetContactByTenantIdAsync(tenantId, ct);
}
