using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueRepository : ITenantScopedRepository<VenueEntity>
{
    Task<VenueEntity?> GetByUserIdAsync(Guid id);
    Task<int?> GetIdByUserIdAsync(Guid userId);
    Task<VenueDetails?> GetDetailsByUserIdAsync(Guid userId);
}
