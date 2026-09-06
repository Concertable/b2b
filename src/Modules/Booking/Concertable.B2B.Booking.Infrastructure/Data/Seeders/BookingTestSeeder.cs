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
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            await context.Database.ExecuteSqlRawAsync(
                $"SET IDENTITY_INSERT [{Schema.Name}].[{Schema.Tables.Bookings}] ON", ct);
            context.Bookings.AddRange(seed.Bookings);
            await context.SaveChangesAsync(ct);
            await context.Database.ExecuteSqlRawAsync(
                $"SET IDENTITY_INSERT [{Schema.Name}].[{Schema.Tables.Bookings}] OFF", ct);

            await context.Database.ExecuteSqlRawAsync(
                $"SET IDENTITY_INSERT [{Schema.Name}].[{Schema.Tables.Contracts}] ON", ct);
            context.Contracts.AddRange(seed.Contracts);
            await context.SaveChangesAsync(ct);
            await context.Database.ExecuteSqlRawAsync(
                $"SET IDENTITY_INSERT [{Schema.Name}].[{Schema.Tables.Contracts}] OFF", ct);

            await transaction.CommitAsync(ct);
        });
}
