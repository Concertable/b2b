using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertRepository : IRepository<ConcertEntity>
{
    Task<ConcertEntity?> GetByBookingIdAsync(int bookingId, CancellationToken ct = default);
    Task<ConcertEntity?> GetWithGrantsByIdAsync(int id, CancellationToken ct = default);
    Task<ConcertState?> GetStateByIdAsync(int concertId, CancellationToken ct = default);
    Task<ConcertSummary?> GetSummaryByIdAsync(int id, CancellationToken ct = default);
    Task<ConcertOperations?> GetOperationsByIdAsync(int id, CancellationToken ct = default);
    Task<ConcertFinance?> GetFinanceByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ConcertDraftReference>> GetDraftReferencesForVenueTenantIdAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<IReadOnlyList<ConcertSummary>> GetUnpostedByArtistIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ConcertSummary>> GetUnpostedByVenueIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForVenueTenantIdAsync(Guid venueTenantId);
    Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForArtistTenantIdAsync(Guid artistTenantId);
    Task<decimal?> GetTotalRevenueByConcertIdAsync(int concertId);
}
