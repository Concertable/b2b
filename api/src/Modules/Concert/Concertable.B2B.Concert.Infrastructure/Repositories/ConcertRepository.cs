using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Contracts.Enums;
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

    public Task<ConcertEntity?> GetWithGrantsByIdAsync(int id, CancellationToken ct = default) =>
        context.Concerts
            .Include(concert => concert.AccessGrants)
            .FirstOrDefaultAsync(concert => concert.Id == id, ct);

    public async Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForVenueTenantIdAsync(Guid venueTenantId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await WithScope(ConcertAccessScope.Operations)
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
        return await WithScope(ConcertAccessScope.Operations)
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

    public Task<ConcertSummary?> GetSummaryByIdAsync(
        int id,
        CancellationToken ct = default) =>
        WithScope(ConcertAccessScope.Summary)
            .Where(concert => concert.Id == id)
            .ToSummary()
            .SingleOrDefaultAsync(ct);

    public Task<ConcertOperations?> GetOperationsByIdAsync(
        int id,
        CancellationToken ct = default) =>
        WithScope(ConcertAccessScope.Operations)
            .Where(concert => concert.Id == id)
            .ToOperations(
                context.ConcertRatingProjections,
                context.ArtistRatingProjections,
                context.VenueRatingProjections)
            .SingleOrDefaultAsync(ct);

    public Task<ConcertFinance?> GetFinanceByIdAsync(
        int id,
        CancellationToken ct = default) =>
        WithScope(ConcertAccessScope.Finance)
            .Where(concert => concert.Id == id)
            .Select(concert => new ConcertFinance(
                concert.Id,
                concert.TicketsSold,
                concert is DoorRevenueConcert ? ((DoorRevenueConcert)concert).DoorRevenue : null,
                concert is DoorRevenueConcert,
                context.Invoices
                    .Where(invoice => invoice.BookingId == concert.BookingId)
                    .Select(invoice => (int?)invoice.Id)
                    .SingleOrDefault(),
                false))
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ConcertDraftReference>> GetDraftReferencesForVenueTenantIdAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        await WithScope(ConcertAccessScope.Operations)
            .Where(concert => concert.VenueTenantId == venueTenantId && concert.DatePosted == null)
            .Select(concert => new ConcertDraftReference(concert.Id, concert.ApplicationId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConcertSummary>> GetUnpostedByArtistIdAsync(
        int id,
        CancellationToken ct = default) =>
        await WithScope(ConcertAccessScope.Summary)
            .Where(e => e.ArtistId == id && e.DatePosted == null)
            .ToSummary()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConcertSummary>> GetUnpostedByVenueIdAsync(
        int id,
        CancellationToken ct = default) =>
        await WithScope(ConcertAccessScope.Summary)
            .Where(e => e.VenueId == id && e.DatePosted == null)
            .ToSummary()
            .ToListAsync(ct);

    public Task<decimal?> GetTotalRevenueByConcertIdAsync(int concertId) =>
        WithScope(ConcertAccessScope.Finance).OfType<DoorRevenueConcert>()
            .Where(c => c.Id == concertId)
            .Select(c => c.TicketsSold * c.Price + c.DoorRevenue)
            .FirstOrDefaultAsync();

    private IQueryable<ConcertEntity> WithScope(ConcertAccessScope scope) =>
        context.Concerts.Where(concert =>
            context.ConcertAccessGrants.Any(grant =>
                grant.ResourceId == concert.Id && grant.Scope == scope));

}
