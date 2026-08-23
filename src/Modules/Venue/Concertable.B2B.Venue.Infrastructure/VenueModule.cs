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

    public Task<Option<int>> GetCurrentIdAsync(CancellationToken ct = default) =>
        venueService.GetCurrentIdAsync(ct);

    public Task<Option<VenueProfile>> GetProfileAsync(
        int venueId,
        CancellationToken ct = default) =>
        venueService.GetProfileAsync(venueId, ct);

    public Task<IReadOnlyList<VenueProfile>> GetProfilesAsync(
        IReadOnlyCollection<int> venueIds,
        CancellationToken ct = default) =>
        venueService.GetProfilesAsync(venueIds, ct);

    public Task<Option<VenueProfile>> GetCurrentProfileAsync(CancellationToken ct = default) =>
        venueService.GetCurrentProfileAsync(ct);
}
