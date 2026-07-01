using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertAvailability(PublicConcertDbContext context) : IConcertAvailability
{
    public Task<bool> OpportunityHasConcertAsync(int opportunityId)
    {
        return context.Concerts.AnyAsync(e => e.Booking.Application.OpportunityId == opportunityId);
    }

    public async Task<bool> ArtistHasConcertOnDateAsync(int artistId, DateTime date)
    {
        return await context.Concerts
            .Where(e => e.Booking.Application.ArtistId == artistId)
            .AnyAsync(e => e.Booking.Application.Opportunity.Period.Start.Date == date.Date);
    }

    public async Task<bool> VenueHasConcertOnDateAsync(int venueId, DateTime date)
    {
        return await context.Concerts
            .Where(e => e.Booking.Application.Opportunity.VenueId == venueId)
            .AnyAsync(e => e.Booking.Application.Opportunity.Period.Start.Date == date.Date);
    }
}
