using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Data.Seeders;

internal sealed class BookingTestSeeder : ITestSeeder
{
    public int Order => 6;

    private readonly BookingDbContext context;
    private readonly SeedState seed;
    private readonly ITenantScope tenantScope;

    public BookingTestSeeder(BookingDbContext context, SeedState seed, ITenantScope tenantScope)
    {
        this.context = context;
        this.seed = seed;
        this.tenantScope = tenantScope;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public Task SeedAsync(CancellationToken ct = default) =>
        context.SeedByVenueTenantAsync(tenantScope, seed.Bookings, ct);
}
