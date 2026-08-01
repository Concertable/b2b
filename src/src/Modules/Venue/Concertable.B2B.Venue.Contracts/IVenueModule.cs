using Concertable.Kernel.Functional;

namespace Concertable.B2B.Venue.Contracts;

public interface IVenueModule
{
    Task<Option<VenueSummary>> GetSummaryAsync(int venueId, CancellationToken ct = default);
    Task<Option<int>> GetVenueIdForCurrentTenantAsync(CancellationToken ct = default);
    Task<Option<VenueOrgIdentity>> GetOrgIdentityByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
