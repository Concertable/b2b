using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Concertable.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Services;

internal sealed class ArtistReviewService : IArtistReviewService
{
    private readonly ArtistDbContext context;
    private readonly IArtistService artistService;

    public ArtistReviewService(ArtistDbContext context, IArtistService artistService)
    {
        this.context = context;
        this.artistService = artistService;
    }

    public async Task<ReviewSummary> GetSummaryAsync(int artistId)
    {
        var projection = await context.ArtistRatingProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ArtistId == artistId);
        return projection.ToReviewSummary();
    }

    public Task<IPagination<ReviewDto>> GetPagedAsync(int artistId, IPageParams pageParams) =>
        context.ArtistReviews
            .AsNoTracking()
            .Where(r => r.ArtistId == artistId)
            .OrderByDescending(r => r.Id)
            .Select(r => new ReviewDto { Id = r.Id, Email = r.Email, Stars = (int)r.Stars, Details = r.Details })
            .ToPaginationAsync(pageParams);

    public async Task<IReadOnlyList<RecentReviewDto>> GetRecentForCurrentAsync(int take)
    {
        var artistId = await artistService.GetIdForCurrentUserAsync();

        return await context.ArtistReviews
            .AsNoTracking()
            .Where(r => r.ArtistId == artistId)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Take(take)
            .Select(r => new RecentReviewDto(
                r.Id,
                r.Email,
                null,
                (int)r.Stars,
                r.Details,
                r.CreatedAt,
                $"/_artist/find/artist/{artistId}"))
            .ToListAsync();
    }
}
