using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueRepository : ITenantScopedRepository<VenueEntity>
{
    Task<VenueEntity?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<VenueDetails?> GetDetailsByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
