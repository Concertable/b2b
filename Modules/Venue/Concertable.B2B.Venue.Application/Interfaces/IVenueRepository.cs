using Concertable.DataAccess.Application;
using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueRepository : IRepository<VenueEntity>
{
    Task<VenueEntity?> GetByUserIdAsync(Guid id);
    Task<int?> GetIdByUserIdAsync(Guid userId);
    Task<VenueSummary?> GetSummaryAsync(int id);
    Task<VenueDetails?> GetDetailsByIdAsync(int id);
    Task<VenueDetails?> GetDetailsByUserIdAsync(Guid userId);
}
