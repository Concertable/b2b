using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertRepository : IRepository<ConcertEntity>
{
    Task<ConcertDetails?> GetDetailsByIdAsync(int id, CancellationToken ct = default);
    Task<ConcertDetails?> GetDetailsByApplicationIdAsync(int applicationId);
    Task<IEnumerable<ConcertSummary>> GetUnpostedByArtistIdAsync(int id);
    Task<IEnumerable<ConcertSummary>> GetUnpostedByVenueIdAsync(int id);
    Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForVenueTenantIdAsync(Guid venueTenantId);
    Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForArtistTenantIdAsync(Guid artistTenantId);
    Task<IEnumerable<int>> GetEndedConfirmedIdsAsync();
    Task<decimal?> GetTotalRevenueByConcertIdAsync(int concertId);
}
