using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertPrivilegedRepository : IRepository<ConcertEntity>
{
    void AddAccessGrants(IEnumerable<ConcertAccessGrant> grants);

    Task<ConcertEntity?> GetByIdForUpdateAsync(int concertId, CancellationToken ct = default);

    Task<ConcertEntity?> GetByBookingIdAsync(int bookingId, CancellationToken ct = default);

    Task<ConcertEntity?> GetWithGrantsByIdAsync(int concertId, CancellationToken ct = default);

    Task<ConcertEntity?> GetWithGrantsByIdForUpdateAsync(int concertId, CancellationToken ct = default);

    Task<ConcertState?> GetStateByIdAsync(
        int concertId,
        CancellationToken ct = default);

}
