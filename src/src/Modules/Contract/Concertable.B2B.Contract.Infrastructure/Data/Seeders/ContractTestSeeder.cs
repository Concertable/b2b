using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Contract.Infrastructure.Data.Seeders;

internal sealed class ContractTestSeeder : ITestSeeder
{
    public int Order => 3;

    private readonly ContractDbContext context;
    private readonly SeedState seed;

    public ContractTestSeeder(ContractDbContext context, SeedState seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Contracts.SeedIfEmptyAsync(async () =>
        {
            context.Contracts.AddRange(seed.Contracts);
            await context.SaveChangesAsync(ct);
        });
}
