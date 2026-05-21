using Concertable.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Repositories;

internal class BookingRepository : Repository<BookingEntity>, IBookingRepository
{
    public BookingRepository(ConcertDbContext context) : base(context) { }

    public async new Task<BookingEntity?> GetByIdAsync(int id)
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
                    .ThenInclude(o => o.OpportunityGenres)
            .Include(b => b.Concert)
            .FirstOrDefaultAsync();
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

    public async Task<BookingEntity?> GetByConcertIdAsync(int concertId)
    {
        return await context.Bookings
            .Where(b => b.Concert!.Id == concertId)
            .Include(b => b.Application)
                .ThenInclude(a => a.Artist)
            .Include(b => b.Application)
                .ThenInclude(a => a.Opportunity)
                    .ThenInclude(o => o.Venue)
            .Include(b => b.Concert)
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
