namespace Concertable.B2B.Venue.Infrastructure;

internal sealed class VenueModule(IVenueRepository repo) : IVenueModule
{
    public Task<VenueSummary?> GetSummaryAsync(int venueId, CancellationToken ct = default) =>
        repo.GetSummaryAsync(venueId);

    public Task<int?> GetVenueIdByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        repo.GetIdByUserIdAsync(userId);
}
