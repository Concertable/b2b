using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Concertable.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal sealed class ArtistRepository(ArtistDbContext context)
    : Repository<ArtistEntity>(context), IArtistRepository
{
    public async Task<ArtistEntity?> GetByUserIdAsync(Guid id) =>
        await context.Artists
            .Where(a => a.UserId == id)
            .FirstOrDefaultAsync();

    public async Task<ArtistEntity?> GetFullByIdAsync(int id) =>
        await context.Artists
            .Where(a => a.Id == id)
            .FirstOrDefaultAsync();

    public async Task<ArtistSummaryDto?> GetSummaryAsync(int id) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.Id == id)
            .ToSummaryDto(context.ArtistRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();

    public async Task<int?> GetIdByUserIdAsync(Guid id) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.UserId == id)
            .Select(a => a.Id)
            .FirstOrDefaultAsync();

    public async Task<ArtistDto?> GetDtoByIdAsync(int id) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.Id == id)
            .ToDto(context.ArtistRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();

    public async Task<ArtistDto?> GetDtoByUserIdAsync(Guid userId) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToDto(context.ArtistRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();

    public async Task<IReadOnlySet<Genre>> GetGenresAsync(int id) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.Id == id)
            .SelectMany(a => a.Genres)
            .ToHashSetAsync();
}
