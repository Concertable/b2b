using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.B2B.Seeding;
using Concertable.B2B.Venue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data.Seeders;

internal class VenueTestSeeder : ITestSeeder
{
    public int Order => 2;

    private readonly VenueDbContext context;
    private readonly SeedData seed;

    public VenueTestSeeder(VenueDbContext context, SeedData seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Venues.SeedIfEmptyAsync(async () =>
        {
            context.Venues.Add(seed.Venue);
            await context.SaveChangesAsync(ct);
        });
}
