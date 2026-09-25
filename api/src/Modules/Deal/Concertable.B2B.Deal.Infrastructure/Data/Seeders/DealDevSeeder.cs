using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Deal.Infrastructure.Data.Seeders;

internal sealed class DealDevSeeder : IDevSeeder
{
    public int Order => 3;

    private readonly DealPrivilegedDbContext context;
    private readonly DealDbContext migrations;
    private readonly SeedState seed;

    public DealDevSeeder(DealPrivilegedDbContext context, DealDbContext migrations, SeedState seed)
    {
        this.context = context;
        this.migrations = migrations;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => migrations.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Deals.SeedIfEmptyAsync(async () =>
        {
            context.Deals.AddRange(seed.Deals);
            await context.SaveChangesAsync(ct);
        });
}
