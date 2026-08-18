using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Data.Seeders;

internal sealed class BookingTestSeeder : ITestSeeder
{
    public int Order => 6;

    private readonly BookingDbContext context;
    private readonly SeedState seed;

    public BookingTestSeeder(BookingDbContext context, SeedState seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Bookings.SeedIfEmptyAsync(async () =>
        {
            seed.LinkBookingsToPersistedApplications();
            context.Bookings.AddRange(seed.Bookings);
            await context.SaveChangesAsync(ct);
        });
}
