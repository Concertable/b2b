using Concertable.DataAccess.Application;
using Concertable.Venue.Application.DTOs;

namespace Concertable.Venue.Application.Interfaces;

internal interface IVenueRepository : IIdRepository<VenueEntity>
{
    Task<VenueEntity?> GetByUserIdAsync(Guid id);
    Task<int?> GetIdByUserIdAsync(Guid userId);
    Task<VenueEntity?> GetFullByIdAsync(int id);
    Task<VenueSummaryDto?> GetSummaryAsync(int id);
    Task<VenueDto?> GetDtoByIdAsync(int id);
    Task<VenueDto?> GetDtoByUserIdAsync(Guid userId);
}
