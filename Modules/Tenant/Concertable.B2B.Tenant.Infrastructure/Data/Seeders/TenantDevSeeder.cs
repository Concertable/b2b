using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Seeders;

internal sealed class TenantDevSeeder : IDevSeeder
{
    public int Order => 1;

    private readonly TenantDbContext context;
    private readonly SeedState seed;

    public TenantDevSeeder(TenantDbContext context, SeedState seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    /* Seeds tenants with deterministic ids up front (before event processing), so seeded venues link to their
       operator tenant without a lookup and the registration handler finds them already present and no-ops. The
       direct insert still routes through TenantEntity.Create, so the same TenantCreatedEvent fires once here. */
    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Tenants.SeedIfEmptyAsync(async () =>
        {
            context.Tenants.AddRange(seed.Tenants);
            await context.SaveChangesAsync(ct);
        });
}
