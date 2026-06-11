using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class BookingRepository : VenueArtistTenantScopedRepository<BookingEntity>, IBookingRepository
{
    public BookingRepository(ConcertDbContext context) : base(context) { }

    public override async Task<BookingEntity?> GetByIdAsync(int id, CancellationToken ct = default)
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

    public async Task<BookingEntity?> GetByApplicationIdAsync(int applicationId)
    {
        return await context.Bookings
            .Where(b => b.ApplicationId == applicationId)
            .Include(b => b.Application)
                .ThenInclude(a => a.Opportunity)
            .Include(b => b.Concert)
            .FirstOrDefaultAsync();
    }

    public async Task<BookingEntity?> GetForSettlementByConcertIdAsync(int concertId)
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

    public Task<int?> GetApplicationIdByIdAsync(int bookingId)
    {
        return context.Bookings
            .Where(b => b.Id == bookingId)
            .Select(b => (int?)b.ApplicationId)
            .FirstOrDefaultAsync();
    }

    public Task<int?> GetContractIdByIdAsync(int bookingId)
    {
        return context.Bookings
            .Where(b => b.Id == bookingId)
            .Select(b => (int?)b.Application.Opportunity.ContractId)
            .FirstOrDefaultAsync();
    }
}
