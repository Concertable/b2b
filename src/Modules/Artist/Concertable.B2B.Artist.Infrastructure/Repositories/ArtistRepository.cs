using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Concertable.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal sealed class ArtistRepository : Repository<ArtistEntity>, IArtistRepository
{
    public ArtistRepository(ArtistDbContext context) : base(context) { }

    public async Task<ArtistEntity?> GetByUserIdAsync(Guid id) =>
        await context.Artists
            .Where(a => a.UserId == id)
            .FirstOrDefaultAsync();

    public async Task<ArtistSummary?> GetSummaryAsync(int id) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.Id == id)
            .ToSummary(context.ArtistRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();

    public async Task<int?> GetIdByUserIdAsync(Guid id) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.UserId == id)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();

    public async Task<ArtistDetails?> GetDetailsByIdAsync(int id) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.Id == id)
            .ToDetails(context.ArtistRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();

    public async Task<ArtistDetails?> GetDetailsByUserIdAsync(Guid userId) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToDetails(context.ArtistRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();

    public async Task<IReadOnlySet<Genre>> GetGenresAsync(int id) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.Id == id)
            .SelectMany(a => a.Genres)
            .ToHashSetAsync();
}
