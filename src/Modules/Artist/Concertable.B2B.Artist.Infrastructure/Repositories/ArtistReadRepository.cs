using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal sealed class ArtistReadRepository : IArtistReadRepository
{
    private readonly IArtistReadDbContext context;

    public ArtistReadRepository(IArtistReadDbContext context)
    {
        this.context = context;
    }

    public async Task<ArtistSummary?> GetSummaryAsync(int id, CancellationToken ct = default) =>
        await context.Artists
            .Where(a => a.Id == id)
            .ToSummary(context.ArtistRatingProjections)
            .FirstOrDefaultAsync(ct);

    public async Task<ArtistDetails?> GetDetailsByIdAsync(int id, CancellationToken ct = default) =>
        await context.Artists
            .Where(a => a.Id == id)
            .ToDetails(context.ArtistRatingProjections)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlySet<Genre>> GetGenresAsync(int id, CancellationToken ct = default) =>
        await context.Artists
            .Where(a => a.Id == id)
            .SelectMany(a => a.Genres)
            .ToHashSetAsync(ct);

    public Task<ArtistProfile?> GetProfileAsync(int id, CancellationToken ct = default) =>
        context.Artists
            .Where(artist => artist.Id == id)
            .ToProfile()
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ArtistSummary>> GetSummariesAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken ct = default) =>
        await context.Artists
            .Where(artist => ids.Contains(artist.Id))
            .ToSummary(context.ArtistRatingProjections)
            .ToListAsync(ct);

    public Task<ArtistProfile?> GetProfileByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        context.Artists
            .Where(artist => artist.TenantId == tenantId)
            .ToProfile()
            .FirstOrDefaultAsync(ct);

    public Task<TenantContact?> GetContactByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        context.Artists
            .Where(artist => artist.TenantId == tenantId)
            .Select(artist => new TenantContact(artist.Name, artist.Email))
            .FirstOrDefaultAsync(ct);
}
