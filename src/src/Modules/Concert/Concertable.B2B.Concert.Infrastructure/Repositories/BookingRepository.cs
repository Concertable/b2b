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

    public async Task<BookingEntity?> GetWithApplicationAndConcertByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.Bookings
            .Where(b => b.Id == id)
            .Include(b => b.Application)
                .ThenInclude(a => a.Artist)
                    .ThenInclude(a => a.Genres)
            .Include(b => b.Application)
                .ThenInclude(a => a.Opportunity)
                    .ThenInclude(o => o.Venue)
            .Include(b => b.Application)
                .ThenInclude(a => a.Opportunity)
            .Include(b => b.Concert)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BookingEntity?> GetByApplicationIdAsync(int applicationId, CancellationToken ct = default)
    {
        return await context.Bookings
            .Where(b => b.ApplicationId == applicationId)
            .Include(b => b.Application)
                .ThenInclude(a => a.Opportunity)
            .Include(b => b.Concert)
            .FirstOrDefaultAsync(ct);
    }

    public Task<BookingEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default) =>
        context.Bookings
            .Include(booking => booking.Application)
            .SingleOrDefaultAsync(booking => booking.Concert!.Id == concertId, ct);

    public async Task<BookingEntity?> GetWithApplicationByConcertIdAsync(int concertId)
    {
        return await context.Bookings
            .Where(b => b.Concert!.Id == concertId)
            .Include(b => b.Application)
                .ThenInclude(a => a.Artist)
            .Include(b => b.Application)
                .ThenInclude(a => a.Opportunity)
                    .ThenInclude(o => o.Venue)
            .FirstOrDefaultAsync();
    }

    public Task<int?> GetIdByConcertIdAsync(int concertId)
    {
        return context.Bookings
            .Where(b => b.Concert!.Id == concertId)
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
            .Select(b => (int?)b.Application.Opportunity.DealId)
            .FirstOrDefaultAsync();
    }
}
