using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class PublicBookingRepository : IPublicBookingRepository
{
    private readonly PublicConcertDbContext context;

    public PublicBookingRepository(PublicConcertDbContext context)
    {
        this.context = context;
    }

    public Task<bool> ExistsAsync(int bookingId) =>
        context.Bookings.AnyAsync(b => b.Id == bookingId);
}
