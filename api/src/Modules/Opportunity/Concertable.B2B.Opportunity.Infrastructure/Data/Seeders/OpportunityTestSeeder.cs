using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Data.Seeders;

internal sealed class OpportunityTestSeeder : ITestSeeder
{
    public int Order => 4;

    private readonly OpportunityDbContext context;
    private readonly SeedState seed;

    public OpportunityTestSeeder(OpportunityDbContext context, SeedState seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Opportunities.SeedIfEmptyAsync(async () =>
        {
            context.Opportunities.AddRange(seed.Opportunities);
            await context.SaveChangesAsync(ct);
        });
}
