using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Data.Seeders;

internal sealed class ConcertTestSeeder : ITestSeeder
{
    public int Order => 7;

    private readonly ConcertDbContext context;
    private readonly SeedState seed;

    public ConcertTestSeeder(
        ConcertDbContext context,
        SeedState seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Concerts.SeedIfEmptyAsync(async () =>
        {
            seed.LinkConcertsToPersistedBookings();
            context.Concerts.AddRange(seed.Concerts);
            await context.SaveChangesAsync(ct);
        });
    }
}
