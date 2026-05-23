using Concertable.Contracts;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Services;

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
