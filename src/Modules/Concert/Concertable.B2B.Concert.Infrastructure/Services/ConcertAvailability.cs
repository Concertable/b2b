using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertAvailability : IConcertAvailability, IApplicationAvailabilityProjection
{
    private readonly IConcertReadDbContext context;

    public ConcertAvailability(IConcertReadDbContext context)
    {
        this.context = context;
    }

    public Task<bool> OpportunityHasConcertAsync(int opportunityId)
    {
        return context.Concerts.AnyAsync(concert => concert.OpportunityId == opportunityId);
    }

    public async Task<bool> ArtistHasConcertOnDateAsync(int artistId, DateTime date)
    {
        return await context.Concerts
            .Where(e => e.ArtistId == artistId)
            .AnyAsync(e => e.Period.Start.Date == date.Date);
    }

    public async Task<bool> VenueHasConcertOnDateAsync(int venueId, DateTime date)
    {
        return await context.Concerts
            .Where(e => e.VenueId == venueId)
            .AnyAsync(e => e.Period.Start.Date == date.Date);
    }
}
