using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.B2B.Venue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data.Seeders;

internal sealed class VenueDevSeeder : IDevSeeder
{
    public int Order => 2;

    private readonly VenueDbContext context;
    private readonly SeedState seed;

    public VenueDevSeeder(VenueDbContext context, SeedState seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Venues.SeedIfEmptyAsync(async () =>
        {
            context.Venues.AddRange(seed.Venues);
            await context.SaveChangesAsync(ct);
        });
}
