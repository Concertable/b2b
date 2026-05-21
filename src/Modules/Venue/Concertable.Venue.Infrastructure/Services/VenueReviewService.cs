using Concertable.Shared;
using Concertable.Venue.Application.Interfaces;
using Concertable.Venue.Infrastructure.Data;
using Concertable.Venue.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Venue.Infrastructure.Services;

internal class VenueReviewService(VenueDbContext context) : IVenueReviewService
{
    public async Task<ReviewSummaryDto> GetSummaryAsync(int venueId)
    {
        var projection = await context.VenueRatingProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.VenueId == venueId);
        return projection.ToReviewSummaryDto();
    }
}
