using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Data.Seeders;

internal class ArtistTestSeeder : ITestSeeder
{
    public int Order => 1;

    private readonly ArtistDbContext context;
    private readonly SeedState seed;

    public ArtistTestSeeder(ArtistDbContext context, SeedState seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Artists.SeedIfEmptyAsync(async () =>
        {
            context.Artists.Add(seed.Artist);
            await context.SaveChangesAsync(ct);
        });
}
