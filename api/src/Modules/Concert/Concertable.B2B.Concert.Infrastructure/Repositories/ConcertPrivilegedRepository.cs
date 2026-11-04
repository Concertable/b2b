using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Locking;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertPrivilegedRepository : PrivilegedRepository<ConcertEntity>, IConcertPrivilegedRepository
{
    private readonly ConcertPrivilegedDbContext context;

    public ConcertPrivilegedRepository(ConcertPrivilegedDbContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<ConcertEntity?> GetByIdWithRowLockAsync(
        int concertId,
        CancellationToken ct = default)
    {
        await context.Concerts.LockRowsAsync(concert => concert.Id == concertId, RowLock.NoKeyUpdate, ct);
        return await context.Concerts.SingleOrDefaultAsync(concert => concert.Id == concertId, ct);
    }

    public Task<ConcertEntity?> GetByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        context.Concerts.SingleOrDefaultAsync(concert => concert.BookingId == bookingId, ct);

    public Task<bool> ExistsByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        context.Concerts.AnyAsync(
            concert => concert.VenueTenantId == tenantId || concert.ArtistTenantId == tenantId,
            ct);

    public Task<ConcertState?> GetStateByIdAsync(
        int concertId,
        CancellationToken ct = default) =>
        context.Concerts
            .Where(concert => concert.Id == concertId)
            .Select(concert => (ConcertState?)concert.State)
            .SingleOrDefaultAsync(ct);
}
