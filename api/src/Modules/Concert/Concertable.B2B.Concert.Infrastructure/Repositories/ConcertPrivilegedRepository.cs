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

    public Task<ConcertEntity?> GetByIdForUpdateAsync(int concertId, CancellationToken ct = default) =>
        context.Concerts.SingleOrDefaultAsync(concert => concert.Id == concertId, ct);

    public Task<ConcertEntity?> GetWithGrantsByIdAsync(int concertId, CancellationToken ct = default) =>
        context.Concerts
            .Include(concert => concert.AccessGrants)
            .SingleOrDefaultAsync(concert => concert.Id == concertId, ct);

    public Task<ConcertAccessIdentity?> GetIdentityByIdForUpdateAsync(int concertId, CancellationToken ct = default) =>
        context.Concerts
            .Where(concert => concert.Id == concertId)
            .Select(concert => new ConcertAccessIdentity(
                concert.Id, concert.VenueTenantId, concert.ArtistTenantId, concert.AccessVersion))
            .SingleOrDefaultAsync(ct);
}
