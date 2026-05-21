namespace Concertable.Venue.Infrastructure;

internal class VenueModule(IVenueRepository repo) : IVenueModule
{
    public Task<VenueSummaryDto?> GetSummaryAsync(int venueId, CancellationToken ct = default) =>
        repo.GetSummaryAsync(venueId);

    public Task<int?> GetVenueIdByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        repo.GetIdByUserIdAsync(userId);
}
