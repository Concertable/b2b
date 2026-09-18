using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Data;

internal sealed class BookingDbContextFactory : B2BDesignTimeDbContextFactory<BookingDbContext>
{
    protected override string MigrationsSchema => Schema.Name;

    protected override BookingDbContext Create(DbContextOptions<BookingDbContext> options) =>
        new(options, DefaultOutboxOptions, new BookingConfigurationProvider(), DesignTimeTenantContext.Instance);
}
