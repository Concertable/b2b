using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertRepository : IRepository<ConcertEntity>
{
    Task<ConcertEntity?> GetByIdWithArtistAndVenueAsync(int id);
    Task<ConcertEntity?> GetByIdWithVenueAsync(int id);
    Task<ConcertEntity?> GetByIdWithBookingAsync(int id);
    Task<ConcertDetails?> GetDetailsByApplicationIdAsync(int applicationId);
    Task<IEnumerable<ConcertSummary>> GetUnpostedByArtistIdAsync(int id);
    Task<IEnumerable<ConcertSummary>> GetUnpostedByVenueIdAsync(int id);
    Task<IEnumerable<int>> GetEndedConfirmedIdsAsync();
    Task<decimal> GetTotalRevenueByConcertIdAsync(int concertId);
    Task<int?> GetContractIdByIdAsync(int concertId);
}
