using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Data.Seeders;

internal sealed class BookingDevSeeder : IDevSeeder
{
    public int Order => 6;

    private readonly BookingPrivilegedDbContext context;
    private readonly BookingDbContext migrations;
    private readonly SeedState seed;

    public BookingDevSeeder(BookingPrivilegedDbContext context, BookingDbContext migrations, SeedState seed)
    {
        this.context = context;
        this.migrations = migrations;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => migrations.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Bookings.SeedIfEmptyAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            context.Bookings.AddRange(seed.Bookings);
            await context.SaveChangesAsync(ct);
            context.Contracts.AddRange(seed.Contracts);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        });
}
