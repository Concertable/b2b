using Concertable.B2B.Concert.Infrastructure.Mappers;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertReadRepository : IConcertReadRepository
{
    private readonly IConcertReadDbContext context;
    private readonly TimeProvider timeProvider;

    public ConcertReadRepository(IConcertReadDbContext context, TimeProvider timeProvider)
    {
        this.context = context;
        this.timeProvider = timeProvider;
    }

    public async Task<ConcertDetails?> GetDetailsByIdAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Id == id)
            .ToDetails(
                context.Applications,
                context.ConcertRatingProjections,
                context.ArtistRatingProjections,
                context.VenueRatingProjections)
            .FirstOrDefaultAsync();
    }

    public async Task<ConcertSummary?> GetSummaryAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Id == id)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ConcertSummary>> GetUpcomingByVenueIdAsync(int venueId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(e => e.VenueId == venueId
                        && e.Period.Start >= now
                        && e.DatePosted != null)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcertSummary>> GetUpcomingByArtistIdAsync(int artistId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(e => e.ArtistId == artistId
                        && e.Period.Start >= now
                        && e.DatePosted != null)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcertSummary>> GetHistoryByVenueIdAsync(int venueId)
    {
        var now = timeProvider.GetUtcNow();
        return await context.Concerts
            .Where(e => e.VenueId == venueId
                        && e.Period.Start < now
                        && e.DatePosted != null)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcertSummary>> GetHistoryByArtistIdAsync(int artistId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(e => e.ArtistId == artistId
                        && e.Period.Start < now
                        && e.DatePosted != null)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }
}
