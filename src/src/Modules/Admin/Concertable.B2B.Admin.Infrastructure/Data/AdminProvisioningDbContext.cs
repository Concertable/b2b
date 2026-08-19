using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Admin.Infrastructure.Data;

internal sealed class AdminProvisioningDbContext(
    DbContextOptions<AdminProvisioningDbContext> options,
    AdminConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<AdminProfileEntity> AdminProfiles => Set<AdminProfileEntity>();
    public DbSet<AdminInvitationEntity> AdminInvitations => Set<AdminInvitationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);
    }
}
