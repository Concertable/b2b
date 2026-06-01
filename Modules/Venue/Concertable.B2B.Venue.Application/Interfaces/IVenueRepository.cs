using Concertable.DataAccess.Application;
using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueRepository : IRepository<VenueEntity>
{
    Task<VenueEntity?> GetByUserIdAsync(Guid id);
    Task<int?> GetIdByUserIdAsync(Guid userId);
    Task<VenueEntity?> GetFullByIdAsync(int id);
    Task<VenueSummaryDto?> GetSummaryAsync(int id);
    Task<VenueDto?> GetDtoByIdAsync(int id);
    Task<VenueDto?> GetDtoByUserIdAsync(Guid userId);
}
