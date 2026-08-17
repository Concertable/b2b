using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Concertable.B2B.Concert.Infrastructure.Specifications;
using Concertable.Kernel.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertRepository : Repository<ConcertEntity>, IConcertRepository
{
    private readonly IEndedSpecification ended;
    private readonly IDoorRevenueOutstandingSpecification doorRevenueOutstanding;

    public ConcertRepository(
        ConcertDbContext context,
        IEndedSpecification ended,
        IDoorRevenueOutstandingSpecification doorRevenueOutstanding) : base(context)
    {
        this.ended = ended;
        this.doorRevenueOutstanding = doorRevenueOutstanding;
    }

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

    public async Task<ConcertEntity?> GetByIdForLifecycleAsync(int id, CancellationToken ct = default)
    {
        return await context.Concerts
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    /* Owner read by concert id. Concert itself is public/unfiltered, so scope by requiring a
       tenant-visible Booking (Bookings is tenant-filtered) — a non-party sees none and gets a 404,
       exactly like ContractRepository.GetByConcertIdAsync. */
    public async Task<ConcertDetails?> GetDetailsByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return await context.Concerts
            .Where(e => e.Id == id && context.Bookings.Any(b => b.Id == e.BookingId))
            .ToDetails(
                context.Applications,
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
                context.Applications,
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

    public Task<int?> GetDealIdByIdAsync(int concertId)
    {
        return context.Concerts
            .Where(c => c.Id == concertId)
            .Select(c => context.Applications
                .Where(a => a.Id == c.ApplicationId)
                .Select(a => context.Opportunities
                    .Where(o => o.Id == a.OpportunityId)
                    .Select(o => (int?)o.DealId)
                    .FirstOrDefault())
                .FirstOrDefault())
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<int>> GetEndedConfirmedIdsAsync() =>
        await ended.And(doorRevenueOutstanding.Not())
            .Apply(context.Concerts.Where(c => context.Applications.Any(a =>
                a.Id == c.ApplicationId && a.State == LifecycleState.Booked)))
            .Select(c => c.Id)
            .ToListAsync();

    /* The gross the artist's revenue share settles against: Concertable's own ticket sales
       (TicketsSold * Price, known) plus the venue-declared external/box-office/cash take
       (DoorRevenue). Null until the venue has declared — DoorRevenue null propagates to null. */
    public Task<decimal?> GetTotalRevenueByConcertIdAsync(int concertId) =>
        context.Concerts
            .Where(c => c.Id == concertId)
            .Select(c => c.TicketsSold * c.Price + c.DoorRevenue)
            .FirstOrDefaultAsync();

}
