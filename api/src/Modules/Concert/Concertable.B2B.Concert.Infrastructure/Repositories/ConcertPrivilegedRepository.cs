using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Domain.Entities;
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

    public Task<ConcertEntity?> GetWithGrantsByIdAsync(int concertId, CancellationToken ct = default) =>
        context.Concerts
            .Include(concert => concert.AccessGrants)
            .SingleOrDefaultAsync(concert => concert.Id == concertId, ct);

    public async Task<ConcertAccessIdentity?> GetIdentityByIdForUpdateAsync(
        int concertId,
        CancellationToken ct = default)
    {
        await this.AcquireUpdateLockAsync(concertId, ct);
        return await context.Concerts
            .Where(concert => concert.Id == concertId)
            .Select(concert => new ConcertAccessIdentity(
                concert.Id, concert.VenueTenantId, concert.ArtistTenantId, concert.AccessVersion))
            .SingleOrDefaultAsync(ct);
    }

    private async Task AcquireUpdateLockAsync(int concertId, CancellationToken ct) =>
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1
            FROM concert.Concerts WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = {concertId}
            """, ct);
}
