using Concertable.Artist.Application.Interfaces;
using Concertable.Artist.Infrastructure.Data;
using Concertable.Artist.Infrastructure.Mappers;
using Concertable.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Artist.Infrastructure.Services;

internal class ArtistReviewService(ArtistDbContext context) : IArtistReviewService
{
    public async Task<ReviewSummaryDto> GetSummaryAsync(int artistId)
    {
        var projection = await context.ArtistRatingProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ArtistId == artistId);
        return projection.ToReviewSummaryDto();
    }
}
