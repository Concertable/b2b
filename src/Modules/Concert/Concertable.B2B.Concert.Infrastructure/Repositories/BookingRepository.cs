using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Specifications;
using Concertable.Kernel.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class BookingRepository : VenueArtistTenantScopedRepository<BookingEntity>, IBookingRepository
{
    private readonly ConcertDbContext context;

    public BookingRepository(ConcertDbContext context) : base(context)
    {
        this.context = context;
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

    public Task<BookingEntity?> GetByConcertIdAsync(
        int concertId,
        ISpecification<BookingEntity> spec,
        CancellationToken ct = default) =>
        context.Bookings
            .Apply(spec)
            .SingleOrDefaultAsync(booking => booking.Concert!.Id == concertId, ct);

    public Task<int?> GetIdByConcertIdAsync(int concertId)
    {
        return context.Bookings
            .Where(b => b.Concert!.Id == concertId)
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();
    }

}
