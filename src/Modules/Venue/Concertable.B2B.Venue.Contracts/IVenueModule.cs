namespace Concertable.B2B.Venue.Contracts;

public interface IVenueModule
{
    Task<VenueSummary> GetSummaryAsync(int venueId, CancellationToken ct = default);
    Task<int?> GetVenueIdForCurrentTenantAsync(CancellationToken ct = default);
    Task<VenueOrgIdentity?> GetOrgIdentityByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
