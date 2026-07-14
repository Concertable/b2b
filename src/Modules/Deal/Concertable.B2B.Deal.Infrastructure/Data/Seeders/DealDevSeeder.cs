using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Deal.Infrastructure.Data.Seeders;

internal sealed class DealDevSeeder : IDevSeeder
{
    public int Order => 3;

    private readonly DealDbContext context;
    private readonly SeedState seed;

    public DealDevSeeder(DealDbContext context, SeedState seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Deals.SeedIfEmptyAsync(async () =>
        {
            context.Deals.AddRange(seed.Deals);
            await context.SaveChangesAsync(ct);
        });
}
