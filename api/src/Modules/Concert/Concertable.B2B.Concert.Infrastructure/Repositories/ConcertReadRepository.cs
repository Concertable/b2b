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
            .ToPublished()
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

    public async Task<IReadOnlyList<PublishedConcert>> GetUpcomingByVenueIdAsync(
        int venueId,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(e => e.VenueId == venueId
                        && e.Period.Start >= now
                        && e.DatePosted != null)
            .ToPublished()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PublishedConcert>> GetUpcomingByArtistIdAsync(
        int artistId,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(e => e.ArtistId == artistId
                        && e.Period.Start >= now
                        && e.DatePosted != null)
            .ToPublished()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PublishedConcert>> GetHistoryByVenueIdAsync(
        int venueId,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        return await context.Concerts
            .Where(e => e.VenueId == venueId
                        && e.Period.Start < now
                        && e.DatePosted != null)
            .ToPublished()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PublishedConcert>> GetHistoryByArtistIdAsync(
        int artistId,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(e => e.ArtistId == artistId
                        && e.Period.Start < now
                        && e.DatePosted != null)
            .ToPublished()
            .ToListAsync(ct);
    }
}
