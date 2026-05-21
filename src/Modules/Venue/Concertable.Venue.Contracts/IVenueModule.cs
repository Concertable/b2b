namespace Concertable.Venue.Contracts;

public interface IVenueModule
{
    Task<VenueSummaryDto?> GetSummaryAsync(int venueId, CancellationToken ct = default);
    Task<int?> GetVenueIdByUserIdAsync(Guid userId, CancellationToken ct = default);
}
