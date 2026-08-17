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
        Profiles().FirstOrDefaultAsync(artist => artist.Id == id, ct);

    public Task<ArtistProfile?> GetProfileByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        Profiles().FirstOrDefaultAsync(artist => artist.TenantId == tenantId, ct);

    private IQueryable<ArtistProfile> Profiles() =>
        context.Artists.Select(artist => new ArtistProfile(
            artist.Id,
            artist.TenantId,
            artist.UserId,
            artist.Name,
            artist.About,
            artist.Email,
            artist.Genres.ToHashSet()));

}
