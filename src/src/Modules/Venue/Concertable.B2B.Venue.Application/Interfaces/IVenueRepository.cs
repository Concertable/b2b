using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueRepository : ITenantScopedRepository<VenueEntity>
{
    Task<int?> GetIdForCurrentTenantAsync();
    Task<VenueDetails?> GetDetailsForCurrentTenantAsync();
}
