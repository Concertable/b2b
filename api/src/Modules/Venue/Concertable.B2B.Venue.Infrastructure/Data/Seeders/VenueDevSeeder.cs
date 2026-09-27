using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.B2B.Venue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data.Seeders;

internal sealed class VenueDevSeeder : IDevSeeder
{
    public int Order => 2;

    private readonly VenuePrivilegedDbContext context;
    private readonly VenueDbContext migrations;
    private readonly SeedState seed;

    public VenueDevSeeder(VenuePrivilegedDbContext context, VenueDbContext migrations, SeedState seed)
    {
        this.context = context;
        this.migrations = migrations;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => migrations.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Venues.SeedIfEmptyAsync(async () =>
        {
            context.Venues.AddRange(seed.Venues);
            await context.SaveChangesAsync(ct);
        });
}
