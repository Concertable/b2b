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
       operator tenant without a lookup. The create event is cleared before insert: publishing it here would race
       the Payment ASB subscription at startup and be dropped (most of it). Registration's Announce() is instead
       the single reliable TenantCreatedEvent trigger (it fires after subscriptions exist), so Payment provisions
       exactly one Stripe account per operator. */
    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Tenants.SeedIfEmptyAsync(async () =>
        {
            foreach (var tenant in seed.Tenants)
                tenant.ClearDomainEvents();
            context.Tenants.AddRange(seed.Tenants);
            // Founding Owner memberships ride alongside tenants (same direct-insert exception). The provisioning
            // handler re-announces idempotently over both — it finds these rows and skips re-creating them.
            context.Memberships.AddRange(seed.Memberships);
            await context.SaveChangesAsync(ct);
        });
}
