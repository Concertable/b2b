using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertPrivilegedRepository : IRepository<ConcertEntity>
{
    Task<ConcertEntity?> GetByIdWithRowLockAsync(int concertId, CancellationToken ct = default);

    Task<ConcertEntity?> GetByBookingIdAsync(int bookingId, CancellationToken ct = default);

    Task<bool> ExistsByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    Task<ConcertState?> GetStateByIdAsync(
        int concertId,
        CancellationToken ct = default);
}
