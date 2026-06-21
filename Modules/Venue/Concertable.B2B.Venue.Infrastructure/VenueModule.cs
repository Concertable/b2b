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

    public Task<VenueSummary> GetSummaryAsync(int venueId, CancellationToken ct = default) =>
        venueService.GetSummaryAsync(venueId);

    public Task<int?> GetVenueIdForCurrentTenantAsync(CancellationToken ct = default) =>
        repository.GetIdForCurrentTenantAsync();
}
