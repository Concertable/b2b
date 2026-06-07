using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertRepository : Repository<ConcertEntity>, IConcertRepository
{
    private readonly TimeProvider timeProvider;

    public ConcertRepository(ConcertDbContext context, TimeProvider timeProvider) : base(context)
    {
        this.timeProvider = timeProvider;
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

    public async Task<ConcertEntity?> GetByIdWithBookingAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Id == id)
            .Include(e => e.Booking)
                .ThenInclude(b => b.Application)
            .FirstOrDefaultAsync();
    }

    public async Task<ConcertSummary?> GetSummaryAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Id == id)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .FirstOrDefaultAsync();
    }

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

    public async Task<IEnumerable<ConcertSummary>> GetUpcomingByVenueIdAsync(int id)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(e => e.Booking.Application.Opportunity.VenueId == id
                        && e.Booking.Application.Opportunity.Period.Start >= now
                        && e.DatePosted != null)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcertSummary>> GetUpcomingByArtistIdAsync(int id)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(e => e.Booking.Application.ArtistId == id
                        && e.Booking.Application.Opportunity.Period.Start >= now
                        && e.DatePosted != null)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<ConcertDetails?> GetDetailsByApplicationIdAsync(int applicationId)
    {
        return await context.Concerts
            .Where(e => e.Booking.ApplicationId == applicationId)
            .ToDetails(
                context.ConcertRatingProjections,
                context.ArtistRatingProjections,
                context.VenueRatingProjections)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ConcertSummary>> GetHistoryByArtistIdAsync(int id)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Concerts
            .Where(e => e.Booking.Application.ArtistId == id
                        && e.Booking.Application.Opportunity.Period.Start < now
                        && e.DatePosted != null)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcertSummary>> GetHistoryByVenueIdAsync(int id)
    {
        var now = timeProvider.GetUtcNow();
        return await context.Concerts
            .Where(e => e.Booking.Application.Opportunity.VenueId == id
                        && e.Booking.Application.Opportunity.Period.Start < now
                        && e.DatePosted != null)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcertSummary>> GetUnpostedByArtistIdAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Booking.Application.ArtistId == id && e.DatePosted == null)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcertSummary>> GetUnpostedByVenueIdAsync(int id)
    {
        return await context.Concerts
            .Where(e => e.Booking.Application.Opportunity.VenueId == id && e.DatePosted == null)
            .ToSummary(context.ArtistRatingProjections, context.VenueRatingProjections)
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
            .Where(c => c.Booking.Application.State == LifecycleState.Booked
                     && c.Period.Start < now)
            .Select(c => c.Id)
            .ToListAsync();
    }

    public Task<decimal> GetTotalRevenueByConcertIdAsync(int concertId) =>
        context.Concerts
            .Where(c => c.Id == concertId)
            .Select(c => c.TicketsSold * c.Price)
            .FirstOrDefaultAsync();

}
