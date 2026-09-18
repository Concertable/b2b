using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Tenant.Infrastructure.Data;

internal sealed class TenantDbContext(
    DbContextOptions<TenantDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    TenantConfigurationProvider provider)
    : DbContextBase(options, outboxOptions)
{
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<TenantMembershipEntity> Memberships => Set<TenantMembershipEntity>();
    public DbSet<TenantInvitationEntity> Invitations => Set<TenantInvitationEntity>();
    public DbSet<TenantActivityEntity> Activities => Set<TenantActivityEntity>();
    public DbSet<TenantVerificationEntity> Verifications => Set<TenantVerificationEntity>();
    public DbSet<VerificationDocumentEntity> VerificationDocuments => Set<VerificationDocumentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);
    }
}
