namespace Concertable.B2B.Venue.Contracts;

public interface IVenueModule
{
    Task<VenueSummary> GetSummaryAsync(int venueId, CancellationToken ct = default);
    Task<int?> GetVenueIdForCurrentTenantAsync(CancellationToken ct = default);
}
