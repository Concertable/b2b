using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Data.Seeders;

internal sealed class ArtistTestSeeder : ITestSeeder
{
    public int Order => 1;

    private readonly ArtistPrivilegedDbContext context;
    private readonly ArtistDbContext migrations;
    private readonly SeedState seed;

    public ArtistTestSeeder(ArtistPrivilegedDbContext context, ArtistDbContext migrations, SeedState seed)
    {
        this.context = context;
        this.migrations = migrations;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => migrations.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Artists.SeedIfEmptyAsync(async () =>
        {
            context.Artists.AddRange(seed.Artists);
            await context.SaveChangesAsync(ct);
        });
}
