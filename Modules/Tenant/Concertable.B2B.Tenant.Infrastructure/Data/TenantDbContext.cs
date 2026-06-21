using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Data;

internal sealed class TenantDbContext(
    DbContextOptions<TenantDbContext> options,
    TenantConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<TenantMembershipEntity> Memberships => Set<TenantMembershipEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);
    }
}
