using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Data.Seeders;

internal sealed class OpportunityTestSeeder : ITestSeeder
{
    public int Order => 4;

    private readonly OpportunityPrivilegedDbContext context;
    private readonly OpportunityDbContext migrations;
    private readonly SeedState seed;

    public OpportunityTestSeeder(OpportunityPrivilegedDbContext context, OpportunityDbContext migrations, SeedState seed)
    {
        this.context = context;
        this.migrations = migrations;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => migrations.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Opportunities.SeedIfEmptyAsync(async () =>
        {
            context.Opportunities.AddRange(seed.Opportunities);
            await context.SaveChangesAsync(ct);
        });
}
