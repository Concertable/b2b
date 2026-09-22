using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Data.Seeders;

internal sealed class ApplicationTestSeeder : ITestSeeder
{
    public int Order => 5;

    private readonly ApplicationDbContext context;
    private readonly SeedState seed;
    private readonly ITenantScope tenantScope;

    public ApplicationTestSeeder(ApplicationDbContext context, SeedState seed, ITenantScope tenantScope)
    {
        this.context = context;
        this.seed = seed;
        this.tenantScope = tenantScope;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.SeedByVenueTenantAsync(tenantScope, seed.Applications, ct);
        await context.SeedByVenueTenantAsync(tenantScope, seed.ConcertAvailabilities, ct);
    }
}
