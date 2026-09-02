using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Concertable.B2B.Concert.Infrastructure.Specifications;
using Concertable.Kernel.Specifications;
using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertRepository : Repository<ConcertEntity>, IConcertRepository
{
    private readonly ConcertDbContext context;
    private readonly IEndedSpecification endedSpecification;
    private readonly IDoorRevenueOutstandingSpecification doorRevenueOutstanding;
    private readonly TimeProvider timeProvider;

    public ConcertRepository(
        ConcertDbContext context,
        IEndedSpecification endedSpecification,
        IDoorRevenueOutstandingSpecification doorRevenueOutstanding,
        TimeProvider timeProvider) : base(context)
    {
        this.context = context;
        this.endedSpecification = endedSpecification;
        this.doorRevenueOutstanding = doorRevenueOutstanding;
        this.timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForVenueTenantIdAsync(Guid venueTenantId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .AsNoTracking()
            .Where(c => c.VenueTenantId == venueTenantId
                        && c.Period.End > now
                        && c.DatePosted != null)
            .OrderBy(c => c.Period.Start)
            .Take(5)
            .Select(c => new ManagerConcertCard(
                c.Id,
                c.Name,
                c.BannerUrl ?? c.Artist.BannerUrl,
                c.Period.Start,
                c.Period.End,
                c.Artist.Name,
                c.TicketsSold,
                c.TotalTickets,
                $"/_venue/my/concerts/concert/{c.Id}"))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForArtistTenantIdAsync(Guid artistTenantId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .AsNoTracking()
            .Where(c => c.ArtistTenantId == artistTenantId
                        && c.Period.End > now
                        && c.DatePosted != null)
            .OrderBy(c => c.Period.Start)
            .Take(5)
            .Select(c => new ManagerConcertCard(
                c.Id,
                c.Name,
                c.BannerUrl ?? c.Artist.BannerUrl,
                c.Period.Start,
                c.Period.End,
                c.Venue.Name,
                c.TicketsSold,
                c.TotalTickets,
                $"/_artist/my/concerts/concert/{c.Id}"))
            .ToListAsync();
    }

    public Task<ConcertEntity?> GetByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default) =>
        context.Concerts.SingleOrDefaultAsync(concert => concert.BookingId == bookingId, ct);

    public Task<ConcertState?> GetStateByIdAsync(
        int concertId,
        CancellationToken ct = default) =>
        context.Concerts
            .Where(concert => concert.Id == concertId)
            .Select(concert => (ConcertState?)concert.State)
            .FirstOrDefaultAsync(ct);

    public async Task<ConcertEntity?> GetByIdWithArtistAndVenueAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Id == id)
            .Include(e => e.Artist)
            .Include(e => e.Venue)
            .FirstOrDefaultAsync();
    }

    public async Task<ConcertEntity?> GetByIdWithVenueAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Id == id)
            .Include(e => e.Venue)
            .FirstOrDefaultAsync();
    }

    public async Task<ConcertDetails?> GetDetailsByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return await context.Concerts
            .Where(e => e.Id == id)
            .ToDetails(
                context.ConcertRatingProjections,
                context.ArtistRatingProjections,
                context.VenueRatingProjections)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ConcertDetails?> GetDetailsByApplicationIdAsync(int applicationId)
    {
        return await context.Concerts
            .Where(e => e.ApplicationId == applicationId)
            .ToDetails(
                context.ConcertRatingProjections,
                context.ArtistRatingProjections,
                context.VenueRatingProjections)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ConcertSummary>> GetUnpostedByArtistIdAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.ArtistId == id && e.DatePosted == null)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcertSummary>> GetUnpostedByVenueIdAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.VenueId == id && e.DatePosted == null)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<int>> GetEndedPendingCompletionIdsAsync(
        CancellationToken ct = default) =>
        await context.Concerts
            .Where(concert =>
                concert.State == ConcertState.Draft ||
                concert.State == ConcertState.Posted ||
                concert.State == ConcertState.SettlementFailed ||
                concert.State == ConcertState.AwaitingSettlement)
            .Where(endedSpecification.And(doorRevenueOutstanding.Not()).ToExpression())
            .Select(c => c.Id)
            .ToListAsync(ct);

    public Task<decimal?> GetTotalRevenueByConcertIdAsync(int concertId) =>
        context.Concerts
            .Where(c => c.Id == concertId)
            .Select(c => c.TicketsSold * c.Price + c.DoorRevenue)
            .FirstOrDefaultAsync();

}
