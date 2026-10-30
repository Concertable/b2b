using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertPrivilegedRepository : PrivilegedRepository<ConcertEntity>, IConcertPrivilegedRepository
{
    private readonly ConcertPrivilegedDbContext context;

    public ConcertPrivilegedRepository(ConcertPrivilegedDbContext context) : base(context)
    {
        this.context = context;
    }

    public void AddAccessGrants(IEnumerable<ConcertAccessGrant> grants) =>
        context.ConcertAccessGrants.AddRange(grants);

    public async Task<ConcertEntity?> GetByIdForUpdateAsync(
        int concertId,
        CancellationToken ct = default)
    {
        await this.AcquireUpdateLockAsync(concertId, ct);
        return await context.Concerts.SingleOrDefaultAsync(concert => concert.Id == concertId, ct);
    }

    public Task<ConcertEntity?> GetByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        context.Concerts.SingleOrDefaultAsync(concert => concert.BookingId == bookingId, ct);

    public Task<ConcertEntity?> GetWithGrantsByIdAsync(int concertId, CancellationToken ct = default) =>
        context.Concerts
            .Include(concert => concert.AccessGrants)
            .SingleOrDefaultAsync(concert => concert.Id == concertId, ct);

    public async Task<ConcertEntity?> GetWithGrantsByIdForUpdateAsync(
        int concertId,
        CancellationToken ct = default)
    {
        await AcquireUpdateLockAsync(concertId, ct);
        return await context.Concerts
            .Include(concert => concert.AccessGrants)
            .SingleOrDefaultAsync(concert => concert.Id == concertId, ct);
    }

    public Task<ConcertState?> GetStateByIdAsync(
        int concertId,
        CancellationToken ct = default) =>
        context.Concerts
            .Where(concert => concert.Id == concertId)
            .Select(concert => (ConcertState?)concert.State)
            .SingleOrDefaultAsync(ct);

    private async Task AcquireUpdateLockAsync(int concertId, CancellationToken ct)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1
            FROM concert."Concerts"
            WHERE "Id" = {concertId}
            FOR UPDATE
            """, ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1
            FROM concert."ConcertAccessGrants"
            WHERE "ResourceId" = {concertId}
            FOR UPDATE
            """, ct);
    }
}
