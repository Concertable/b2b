using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Authorization.Contracts;
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
        return await Operable()
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
        return await Operable()
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
        context.Concerts
            .Where(concert => concert.Id == id)
            .ToSummary()
            .SingleOrDefaultAsync(ct);

    public Task<ConcertOperations?> GetOperationsByIdAsync(
        int id,
        CancellationToken ct = default) =>
        Operable()
            .Where(concert => concert.Id == id)
            .ToOperations(
                context.ConcertRatingProjections,
                context.ArtistRatingProjections,
                context.VenueRatingProjections)
            .SingleOrDefaultAsync(ct);

    public Task<ConcertFinance?> GetFinanceByIdAsync(
        int id,
        CancellationToken ct = default) =>
        Settleable()
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
        await Operable()
            .Where(concert => concert.VenueTenantId == venueTenantId && concert.DatePosted == null)
            .Select(concert => new ConcertDraftReference(concert.Id, concert.ApplicationId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConcertSummary>> GetUnpostedByArtistIdAsync(
        int id,
        CancellationToken ct = default) =>
        await context.Concerts
            .Where(e => e.ArtistId == id && e.DatePosted == null)
            .ToSummary()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConcertSummary>> GetUnpostedByVenueIdAsync(
        int id,
        CancellationToken ct = default) =>
        await context.Concerts
            .Where(e => e.VenueId == id && e.DatePosted == null)
            .ToSummary()
            .ToListAsync(ct);

    public Task<decimal?> GetTotalRevenueByConcertIdAsync(int concertId) =>
        Settleable().OfType<DoorRevenueConcert>()
            .Where(c => c.Id == concertId)
            .Select(c => c.TicketsSold * c.Price + c.DoorRevenue)
            .FirstOrDefaultAsync();

    public Task<bool> CanDeclareDoorRevenueByIdAsync(int id, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return Settleable()
            .AnyAsync(concert => concert.Id == id
                && (concert is DoorSplitConcert && ((DoorSplitConcert)concert).DoorRevenue == null
                    || concert is VersusConcert && ((VersusConcert)concert).DoorRevenue == null)
                && concert.Period.End < now
                && (concert.State == ConcertState.Draft || concert.State == ConcertState.Posted), ct);
    }

    private IQueryable<ConcertEntity> Operable()
    {
        if (context.Actor is not { } actor)
            return context.Concerts.Where(_ => false);

        var tenantWide = actor.Permissions[TenantPermission.OperationsView] == ResourceAccess.All;
        var tenantId = actor.TenantId;
        var membershipId = actor.MembershipId;
        return context.Concerts.Where(concert =>
            tenantWide && (concert.VenueTenantId == tenantId || concert.ArtistTenantId == tenantId)
            || concert.Assignments.Any(assignment => assignment.MembershipId == membershipId));
    }

    private IQueryable<ConcertEntity> Settleable()
    {
        if (context.Actor is not { } actor
            || actor.Permissions[TenantPermission.SettlementView] != ResourceAccess.All)
            return context.Concerts.Where(_ => false);

        var tenantId = actor.TenantId;
        return context.Concerts.IgnoreQueryFilters([TenantFilters.Key])
            .Where(concert => concert.VenueTenantId == tenantId || concert.ArtistTenantId == tenantId);
    }

}
