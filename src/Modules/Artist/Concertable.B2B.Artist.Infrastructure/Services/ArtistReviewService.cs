using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Concertable.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Services;

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
