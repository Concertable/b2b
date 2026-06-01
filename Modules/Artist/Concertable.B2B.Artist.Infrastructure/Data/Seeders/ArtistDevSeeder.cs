using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Data.Seeders;

internal sealed class ArtistDevSeeder : IDevSeeder
{
    public int Order => 1;

    private readonly ArtistDbContext context;
    private readonly SeedState seed;

    public ArtistDevSeeder(ArtistDbContext context, SeedState seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Artists.SeedIfEmptyAsync(async () =>
        {
            context.Artists.AddRange(seed.Artists);
            await context.SaveChangesAsync(ct);
        });
}
