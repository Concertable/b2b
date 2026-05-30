using Concertable.Seed;
using Concertable.Seed.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Contract.Infrastructure.Data.Seeders;

internal class ContractDevSeeder : IDevSeeder
{
    public int Order => 3;

    private readonly ContractDbContext context;
    private readonly SeedData seed;

    public ContractDevSeeder(ContractDbContext context, SeedData seed)
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
