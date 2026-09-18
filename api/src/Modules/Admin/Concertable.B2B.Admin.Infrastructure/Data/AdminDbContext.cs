using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Admin.Infrastructure.Data;

internal sealed class AdminDbContext(
    DbContextOptions<AdminDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    AdminConfigurationProvider provider)
    : DbContextBase(options, outboxOptions)
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
