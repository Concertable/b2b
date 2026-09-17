using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Concertable.B2B.Concert.Infrastructure.Specifications;
using Concertable.Kernel.Specifications;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertReadRepository : IConcertReadRepository
{
    private readonly IConcertReadDbContext context;
    private readonly IEndedSpecification endedSpecification;
    private readonly IDoorRevenueOutstandingSpecification doorRevenueOutstanding;
    private readonly TimeProvider timeProvider;

    public ConcertReadRepository(
        IConcertReadDbContext context,
        IEndedSpecification endedSpecification,
        IDoorRevenueOutstandingSpecification doorRevenueOutstanding,
        TimeProvider timeProvider)
    {
        this.context = context;
        this.endedSpecification = endedSpecification;
        this.doorRevenueOutstanding = doorRevenueOutstanding;
        this.timeProvider = timeProvider;
    }

    public Task<PublishedConcert?> GetPublishedByIdAsync(int id, CancellationToken ct = default) =>
        context.Concerts
            .Where(concert => concert.Id == id && concert.DatePosted != null)
            .Select(concert => new PublishedConcert(
                concert.Id,
                concert.Name,
                concert.About,
                concert.Period.Start,
                concert.Period.End,
                concert.Venue.Name,
                concert.Artist.Name,
                concert.Price))
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<int>> GetEndedPendingCompletionIdsAsync(
        DateTime endedBeforeUtc, int take, CancellationToken ct = default) =>
        await context.Concerts
            .Where(concert =>
                concert.State == ConcertState.Draft ||
                concert.State == ConcertState.Posted ||
                concert.State == ConcertState.SettlementFailed ||
                concert.State == ConcertState.AwaitingSettlement)
            .Where(concert => concert.Period.End <= endedBeforeUtc)
            .Where(endedSpecification.And(doorRevenueOutstanding.Not()).ToExpression())
            .OrderBy(concert => concert.Period.End)
            .Take(take)
            .Select(concert => concert.Id)
            .ToListAsync(ct);

    public async Task<ConcertDetails?> GetDetailsByIdAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Id == id)
            .ToDetails(
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
