using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Data;

internal sealed class BookingDbContextFactory : B2BDesignTimeDbContextFactory<BookingDbContext>
{
    protected override BookingDbContext Create(DbContextOptions<BookingDbContext> options) =>
        new(options, new BookingConfigurationProvider(), DesignTimeTenantContext.Instance);
}
