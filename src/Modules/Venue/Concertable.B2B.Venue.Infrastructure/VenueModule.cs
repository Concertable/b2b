namespace Concertable.B2B.Venue.Infrastructure;

internal sealed class VenueModule : IVenueModule
{
    private readonly IVenueService venueService;
    private readonly IVenueReviewService reviewService;

    public VenueModule(
        IVenueService venueService,
        IVenueReviewService reviewService)
    {
        this.venueService = venueService;
        this.reviewService = reviewService;
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

    public Task<ReviewSummary> GetReviewSummaryAsync(
        int venueId,
        CancellationToken ct = default) =>
        reviewService.GetSummaryAsync(venueId, ct);
}
