using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

/// <summary>
/// Adds unfiltered, no-tracking read-back over the Tenant module's tables — so tests assert the persisted
/// membership/tenant rows directly instead of resolving a context off the service provider per test.
/// </summary>
public sealed class TenantApiFixture : ApiFixture
{
    private TenantDbContext tenantDb = null!;

    public IQueryable<TenantEntity> Tenants => tenantDb.Tenants.AsNoTracking();
    public IQueryable<TenantMembershipEntity> Memberships => tenantDb.Memberships.AsNoTracking();

    /// <summary>Grants <paramref name="userId"/> an Owner membership in <paramref name="tenantId"/> — lets a test
    /// arrange the multi-membership case the seed graph never holds (every seeded operator owns one tenant).</summary>
    public async Task AddOwnerMembershipAsync(Guid tenantId, Guid userId)
    {
        tenantDb.Memberships.Add(
            TenantMembershipEntity.Create(tenantId, userId, TenantRole.Owner, invitedBy: null, DateTime.UtcNow));
        await tenantDb.SaveChangesAsync();
    }

    protected override void OnReset(IServiceScope scope)
    {
        tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    }
}
