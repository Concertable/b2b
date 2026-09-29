using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal sealed class ArtistPrivilegedRepository(
    ArtistPrivilegedDbContext context,
    CommandTransactionAccessor transactions) : IArtistPrivilegedRepository
{
    public async Task<ArtistProfile?> GetByIdAsync(int artistId, CancellationToken ct = default)
    {
        await EnlistAsync(ct);
        return await context.Artists
            .Where(artist => artist.Id == artistId)
            .ToProfile()
            .SingleOrDefaultAsync(ct);
    }

    public async Task<ArtistProfile?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        await EnlistAsync(ct);
        return await context.Artists
            .Where(artist => artist.TenantId == tenantId)
            .ToProfile()
            .SingleOrDefaultAsync(ct);
    }

    public async Task<ArtistSummary?> GetSummaryByIdAsync(
        int artistId,
        CancellationToken ct = default)
    {
        await EnlistAsync(ct);
        return await context.Artists
            .Where(artist => artist.Id == artistId)
            .ToSummary(context.ArtistRatingProjections)
            .SingleOrDefaultAsync(ct);
    }

    private Task EnlistAsync(CancellationToken ct) =>
        (transactions.Current
            ?? throw new InvalidOperationException("Artist privileged queries require an active transaction."))
        .EnlistAsync(context, ct);
}
