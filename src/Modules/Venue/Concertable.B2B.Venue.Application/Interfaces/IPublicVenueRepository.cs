using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.Application.Interfaces;

/// <summary>
/// The public marketplace surface over venues — any venue's summary/details page, read with the
/// "Tenant" filter lifted (the venue row is public). Owner/host reads live on <see cref="IVenueRepository"/>.
/// </summary>
internal interface IPublicVenueRepository
{
    Task<VenueSummary?> GetSummaryAsync(int id, CancellationToken ct = default);
    Task<VenueDetails?> GetDetailsByIdAsync(int id, CancellationToken ct = default);
    Task<VenueOrgIdentity?> GetOrgIdentityByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default);
}
