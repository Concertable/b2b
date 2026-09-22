using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Seed.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Concert.Infrastructure.Data.Seeders;

internal sealed class ConcertDevSeeder : IDevSeeder
{
    public int Order => 7;

    private readonly ConcertDbContext context;
    private readonly SeedState seed;
    private readonly ITenantModule tenants;
    private readonly ITenantScope tenantScope;
    private readonly LegalSettings legal;
    private readonly TimeProvider timeProvider;

    public ConcertDevSeeder(
        ConcertDbContext context,
        SeedState seed,
        ITenantModule tenants,
        ITenantScope tenantScope,
        IOptions<LegalSettings> legal,
        TimeProvider timeProvider)
    {
        this.context = context;
        this.seed = seed;
        this.tenants = tenants;
        this.tenantScope = tenantScope;
        this.legal = legal.Value;
        this.timeProvider = timeProvider;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.SeedByVenueTenantAsync(tenantScope, seed.Concerts, ct);

        var agreements = await SeededSelfBillingAgreementGranter.GrantAsync(
            seed, tenants, legal.PlatformTermsVersion, timeProvider.GetUtcNow().UtcDateTime, ct);

        await context.SeedByTenantAsync(tenantScope, agreements, ct);
    }
}
