using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal sealed class ArtistReviewRepository : ReadRepository<ArtistReview>, IArtistReviewRepository
{
    private readonly ArtistDbContext artistContext;

    public ArtistReviewRepository(ArtistDbContext context) : base(context)
    {
        artistContext = context;
    }

    public Task<ArtistRatingProjection?> GetRatingByArtistIdAsync(
        int artistId,
        CancellationToken ct = default) =>
        artistContext.ArtistRatingProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ArtistId == artistId, ct);

    public Task<IPagination<ArtistReview>> GetPagedByArtistIdAsync(int artistId, IPageParams pageParams) =>
        artistContext.ArtistReviews
            .AsNoTracking()
            .Where(r => r.ArtistId == artistId)
            .OrderByDescending(r => r.Id)
            .ToPaginationAsync(pageParams);

    public async Task<IReadOnlyList<ArtistReview>> GetRecentByArtistIdAsync(
        int artistId,
        int take,
        CancellationToken ct = default) =>
        await artistContext.ArtistReviews
            .AsNoTracking()
            .Where(r => r.ArtistId == artistId)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Take(take)
            .ToListAsync(ct);
}
