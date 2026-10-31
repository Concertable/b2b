using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertPrivilegedRepository : PrivilegedRepository<ConcertEntity>, IConcertPrivilegedRepository
{
    private readonly ConcertPrivilegedDbContext context;

    public ConcertPrivilegedRepository(ConcertPrivilegedDbContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<ConcertEntity?> GetByIdForUpdateAsync(
        int concertId,
        CancellationToken ct = default)
    {
        await this.AcquireUpdateLockAsync(concertId, ct);
        return await context.Concerts.SingleOrDefaultAsync(concert => concert.Id == concertId, ct);
    }

    public Task<ConcertEntity?> GetByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        context.Concerts.SingleOrDefaultAsync(concert => concert.BookingId == bookingId, ct);

    public Task<ConcertState?> GetStateByIdAsync(
        int concertId,
        CancellationToken ct = default) =>
        context.Concerts
            .Where(concert => concert.Id == concertId)
            .Select(concert => (ConcertState?)concert.State)
            .SingleOrDefaultAsync(ct);

    private Task<int> AcquireUpdateLockAsync(int concertId, CancellationToken ct) =>
        context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1
            FROM concert."Concerts"
            WHERE "Id" = {concertId}
            FOR UPDATE
            """, ct);
}
