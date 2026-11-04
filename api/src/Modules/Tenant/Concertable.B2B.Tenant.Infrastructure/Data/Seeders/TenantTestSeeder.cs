using Concertable.B2B.Authorization.Contracts;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Seeders;

internal sealed class TenantTestSeeder : ITestSeeder
{
    public int Order => 1;

    private readonly TenantDbContext context;
    private readonly SeedState seed;

    public TenantTestSeeder(TenantDbContext context, SeedState seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var catalogState = await context.AuthorizationCatalogStates.SingleOrDefaultAsync(ct);
        if (catalogState is null)
            context.AuthorizationCatalogStates.Add(AuthorizationCatalogState.Create(AuthorizationCatalog.Revision));
        else
            catalogState.Install(AuthorizationCatalog.Revision);
        await context.SaveChangesAsync(ct);
        await context.Tenants.SeedIfEmptyAsync(async () =>
        {
            context.Tenants.AddRange(seed.Tenants);
            context.RoleDefinitions.AddRange(seed.Tenants.SelectMany(tenant => TenantRoleProvisioning.CreatePresets(tenant.Id)));
            context.Memberships.AddRange(seed.Memberships);
            context.Verifications.AddRange(seed.Verifications);
            await context.SaveChangesAsync(ct);
        });
    }
}
