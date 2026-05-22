using Microsoft.EntityFrameworkCore;

namespace Concertable.Organization.Infrastructure.Data;

internal class OrganizationDbContext(
    DbContextOptions<OrganizationDbContext> options,
    OrganizationConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);
    }
}
