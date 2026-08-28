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

    public async Task<TenantContact?> GetContactByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        await context.Artists
            .Where(a => a.TenantId == tenantId)
            .Select(a => (TenantContact?)new TenantContact(a.Name, a.Email))
            .FirstOrDefaultAsync(ct);
}
