using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class BookingRepository : VenueArtistTenantScopedRepository<BookingEntity>, IBookingRepository
{
    private readonly ConcertDbContext context;

    public BookingRepository(ConcertDbContext context) : base(context)
    {
        this.context = context;
    }

    public override async Task<BookingEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await context.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<BookingEntity?> GetByApplicationIdAsync(int applicationId, CancellationToken ct = default)
        => await context.Bookings.FirstOrDefaultAsync(b => b.ApplicationId == applicationId, ct);

    public Task<BookingDraftContext?> GetDraftContextByIdAsync(int bookingId, CancellationToken ct = default) =>
        (from booking in context.Bookings
         join artist in context.ArtistReadModels on booking.ArtistId equals artist.Id
         join opportunity in context.Opportunities on booking.OpportunityId equals opportunity.Id
         join venue in context.VenueReadModels on opportunity.VenueId equals venue.Id
         where booking.Id == bookingId
         select new BookingDraftContext(booking, artist, opportunity, venue))
        .FirstOrDefaultAsync(ct);

    public Task<BookingEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default) =>
        context.Bookings
            .SingleOrDefaultAsync(booking =>
                context.Concerts.Any(concert => concert.Id == concertId && concert.BookingId == booking.Id), ct);

    public async Task<BookingEntity?> GetForSettlementByConcertIdAsync(int concertId)
    {
        return await context.Bookings.FirstOrDefaultAsync(b =>
            context.Concerts.Any(concert => concert.Id == concertId && concert.BookingId == b.Id));
    }

    public Task<int?> GetIdByConcertIdAsync(int concertId)
    {
        return context.Bookings
            .Where(b => context.Concerts.Any(concert => concert.Id == concertId && concert.BookingId == b.Id))
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();
    }

    public Task<int?> GetApplicationIdByIdAsync(int bookingId, CancellationToken ct = default)
    {
        return context.Bookings
            .Where(b => b.Id == bookingId)
            .Select(b => (int?)b.ApplicationId)
            .FirstOrDefaultAsync(ct);
    }

    public Task<int?> GetDealIdByIdAsync(int bookingId)
    {
        return context.Bookings
            .Where(b => b.Id == bookingId)
            .Select(b => context.Opportunities
                .Where(o => o.Id == b.OpportunityId)
                .Select(o => (int?)o.DealId)
                .FirstOrDefault())
            .FirstOrDefaultAsync();
    }
}
