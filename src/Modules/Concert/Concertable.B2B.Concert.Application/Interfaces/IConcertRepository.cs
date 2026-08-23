using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertRepository : IRepository<ConcertEntity>
{
    Task<ConcertEntity?> GetByBookingIdAsync(int bookingId, CancellationToken ct = default);
    Task<ConcertEntity?> GetByIdWithArtistAndVenueAsync(int id);
    Task<ConcertEntity?> GetByIdWithVenueAsync(int id);
    Task<ConcertDetails?> GetDetailsByIdAsync(int id, CancellationToken ct = default);
    Task<ConcertDetails?> GetDetailsByApplicationIdAsync(int applicationId);
    Task<IEnumerable<ConcertSummary>> GetUnpostedByArtistIdAsync(int id);
    Task<IEnumerable<ConcertSummary>> GetUnpostedByVenueIdAsync(int id);
    Task<IReadOnlyList<int>> GetEndedPendingCompletionIdsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForVenueTenantIdAsync(Guid venueTenantId);
    Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForArtistTenantIdAsync(Guid artistTenantId);
    Task<decimal?> GetTotalRevenueByConcertIdAsync(int concertId);
}
