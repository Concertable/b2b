using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Data;

internal sealed class BookingReadDbContext(
    DbContextOptions<BookingReadDbContext> options,
    BookingConfigurationProvider provider)
    : ReadDbContext(options, provider, Schema.Name), IBookingReadDbContext
{
    IQueryable<BookingEntity> IBookingReadDbContext.Bookings => Query<BookingEntity>();
    IQueryable<ContractEntity> IBookingReadDbContext.Contracts => Query<ContractEntity>();
}
