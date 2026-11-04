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
        var fingerprintState = await context.SystemRolesFingerprints.SingleOrDefaultAsync(ct);
        if (fingerprintState is null)
            context.SystemRolesFingerprints.Add(SystemRolesFingerprint.Create(SystemRoles.Fingerprint));
        else
            fingerprintState.Replace(SystemRoles.Fingerprint);
        await context.SaveChangesAsync(ct);
        await context.Tenants.SeedIfEmptyAsync(async () =>
        {
            context.Tenants.AddRange(seed.Tenants);
            context.RoleDefinitions.AddRange(seed.Tenants.SelectMany(tenant => TenantRoleProvisioning.CreatePresets(
                tenant.Id, tenant.BusinessActivities.Where(activity => activity.IsActive).Select(activity => activity.Kind))));
            context.Memberships.AddRange(seed.Memberships);
            context.Verifications.AddRange(seed.Verifications);
            await context.SaveChangesAsync(ct);
        });
    }
}
