using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal sealed class PublicArtistRepository(PublicArtistDbContext context) : IPublicArtistRepository
{
    public async Task<ArtistSummary?> GetSummaryAsync(int id) =>
        await context.Artists
            .Where(a => a.Id == id)
            .ToSummary(context.ArtistRatingProjections)
            .FirstOrDefaultAsync();

    public async Task<ArtistDetails?> GetDetailsByIdAsync(int id) =>
        await context.Artists
            .Where(a => a.Id == id)
            .ToDetails(context.ArtistRatingProjections)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlySet<Genre>> GetGenresAsync(int id) =>
        await context.Artists
            .Where(a => a.Id == id)
            .SelectMany(a => a.Genres)
            .ToHashSetAsync();
}
