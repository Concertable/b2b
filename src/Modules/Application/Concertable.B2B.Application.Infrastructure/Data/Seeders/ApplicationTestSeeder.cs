using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Data.Seeders;

internal sealed class ApplicationTestSeeder : ITestSeeder
{
    public int Order => 5;

    private readonly ApplicationDbContext context;
    private readonly SeedState seed;

    public ApplicationTestSeeder(
        ApplicationDbContext context,
        SeedState seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await SeedStateAsync(ct);

    private async Task SeedStateAsync(CancellationToken ct)
    {
        await context.Applications.SeedIfEmptyAsync(async () =>
        {
            context.Applications.AddRange(seed.Applications);
            await context.SaveChangesAsync(ct);
        });

        await context.ConcertAvailabilities.SeedIfEmptyAsync(async () =>
        {
            context.ConcertAvailabilities.AddRange(seed.ConcertAvailabilities);
            await context.SaveChangesAsync(ct);
        });
    }
}
