using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueReadRepository
{
    Task<VenueSummary?> GetSummaryAsync(int id, CancellationToken ct = default);
    Task<VenueDetails?> GetDetailsByIdAsync(int id, CancellationToken ct = default);
    Task<VenueProfile?> GetProfileAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<VenueProfile>> GetProfilesAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken ct = default);
    Task<VenueProfile?> GetProfileByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
