using Concertable.Seed;
using Concertable.Seed.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Data.Seeders;

internal class ArtistTestSeeder : ITestSeeder
{
    public int Order => 1;

    private readonly ArtistDbContext context;
    private readonly SeedData seed;

    public ArtistTestSeeder(ArtistDbContext context, SeedData seed)
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
