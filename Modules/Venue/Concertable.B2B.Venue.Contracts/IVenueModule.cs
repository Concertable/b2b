namespace Concertable.B2B.Venue.Contracts;

public interface IVenueModule
{
    Task<VenueSummary> GetSummaryAsync(int venueId, CancellationToken ct = default);
    Task<int?> GetVenueIdByUserIdAsync(Guid userId, CancellationToken ct = default);
}
