using Concertable.Concert.Infrastructure.Data;
using Concertable.Concert.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Repositories;

internal class ConcertRepository : Repository<ConcertEntity>, IConcertRepository
{
    private readonly TimeProvider timeProvider;

    public ConcertRepository(ConcertDbContext context, TimeProvider timeProvider) : base(context)
    {
        this.timeProvider = timeProvider;
    }

    public async Task<ConcertEntity?> GetFullByIdAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Id == id)
            .Include(e => e.Booking)
                .ThenInclude(b => b.Application)
                    .ThenInclude(ca => ca.Artist)
                        .ThenInclude(a => a.Genres)
            .Include(e => e.Booking)
                .ThenInclude(b => b.Application)
                    .ThenInclude(a => a.Opportunity)
                .ThenInclude(o => o.Venue)
            .FirstOrDefaultAsync();
    }

    public async Task<ConcertSummaryDto?> GetSummaryAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Id == id)
            .ToSummaryDto(context.ArtistRatingProjections, context.VenueRatingProjections)
            .FirstOrDefaultAsync();
    }

    public async Task<ConcertDto?> GetDtoByIdAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Id == id)
            .ToDto(
                context.ConcertRatingProjections,
                context.ArtistRatingProjections,
                context.VenueRatingProjections)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ConcertSummaryDto>> GetUpcomingByVenueIdAsync(int id)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(e => e.Booking.Application.Opportunity.VenueId == id
                        && e.Booking.Application.Opportunity.Period.Start >= now
                        && e.DatePosted != null)
            .ToSummaryDto(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcertSummaryDto>> GetUpcomingByArtistIdAsync(int id)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(e => e.Booking.Application.ArtistId == id
                        && e.Booking.Application.Opportunity.Period.Start >= now
                        && e.DatePosted != null)
            .ToSummaryDto(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<ConcertDto?> GetDtoByApplicationIdAsync(int applicationId)
    {
        return await context.Concerts
            .Where(e => e.Booking.ApplicationId == applicationId)
            .ToDto(
                context.ConcertRatingProjections,
                context.ArtistRatingProjections,
                context.VenueRatingProjections)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ConcertSummaryDto>> GetHistoryByArtistIdAsync(int id)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(e => e.Booking.Application.ArtistId == id
                        && e.Booking.Application.Opportunity.Period.Start < now
                        && e.DatePosted != null)
            .ToSummaryDto(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcertSummaryDto>> GetHistoryByVenueIdAsync(int id)
    {
        var now = timeProvider.GetUtcNow();
        return await context.Concerts
            .Where(e => e.Booking.Application.Opportunity.VenueId == id
                        && e.Booking.Application.Opportunity.Period.Start < now
                        && e.DatePosted != null)
            .ToSummaryDto(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcertSummaryDto>> GetUnpostedByArtistIdAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Booking.Application.ArtistId == id && e.DatePosted == null)
            .ToSummaryDto(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcertSummaryDto>> GetUnpostedByVenueIdAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Booking.Application.Opportunity.VenueId == id && e.DatePosted == null)
            .ToSummaryDto(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<bool> ArtistHasConcertOnDateAsync(int artistId, DateTime date)
    {
        return await context.Concerts
            .Where(e => e.Booking.Application.ArtistId == artistId)
            .AnyAsync(e => e.Booking.Application.Opportunity.Period.Start.Date == date.Date);
    }

    public Task<bool> OpportunityHasConcertAsync(int opportunityId)
    {
        return context.Concerts.AnyAsync(e => e.Booking.Application.OpportunityId == opportunityId);
    }

    public async Task<bool> VenueHasConcertOnDateAsync(int venueId, DateTime date)
    {
        return await context.Concerts
            .Where(e => e.Booking.Application.Opportunity.VenueId == venueId)
            .AnyAsync(e => e.Booking.Application.Opportunity.Period.Start.Date == date.Date);
    }

    public Task<int?> GetContractIdByIdAsync(int concertId)
    {
        return context.Concerts
            .Where(c => c.Id == concertId)
            .Select(c => (int?)c.Booking.Application.Opportunity.ContractId)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<int>> GetEndedConfirmedIdsAsync()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(c => c.Booking.Application.Status == ApplicationStatus.Accepted
                     && c.Booking.Status == BookingStatus.Confirmed
                     && c.Period.Start < now)
            .Select(c => c.Id)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalRevenueByConcertIdAsync(int concertId)
    {
        // BROKEN Phase 1: sold count is now Customer.Concert.ConcertEntity (TotalTickets - AvailableTickets).
        // Cross-IVT to Customer.Concert here, or query the future ConcertSalesProjection populated from
        // TicketPurchasedEvent. Returning 0 until Phase 2.
        var price = await context.Concerts
            .Where(c => c.Id == concertId)
            .Select(c => c.Price)
            .FirstOrDefaultAsync();
        return 0m;
    }

}
