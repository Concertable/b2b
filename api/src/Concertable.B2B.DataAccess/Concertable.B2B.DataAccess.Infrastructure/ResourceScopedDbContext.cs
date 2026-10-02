using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class ResourceScopedDbContext : TenantScopedDbContext, IHasResourceAccessContext
{
    public IResourceAccessContext ResourceAccess { get; }

    public DbSet<MembershipAuthority> MembershipAuthority => Set<MembershipAuthority>();

    public Guid? ActiveMembershipId => ResourceAccess.Membership?.MembershipId;
    public Guid? ActiveTenantId => ResourceAccess.Membership?.TenantId;
    public Guid? ActiveUserId => ResourceAccess.Membership?.UserId;
    public long? ActivePermissionVersion => ResourceAccess.Membership?.PermissionVersion;

    public ResourceAudience AudienceFor(TenantPermission permission) => ResourceAccess.AudienceFor(permission);

    protected ResourceScopedDbContext(
        DbContextOptions options,
        IOptions<OutboxOptions> outboxOptions,
        IEntityTypeConfigurationProvider provider,
        ITenantContext tenantContext,
        IResourceAccessContext resourceAccess,
        string defaultSchema)
        : base(options, outboxOptions, provider, tenantContext, defaultSchema)
    {
        ResourceAccess = resourceAccess;
    }

    protected override void ConfigureMembershipAuthority(ModelBuilder modelBuilder)
    {
        base.ConfigureMembershipAuthority(modelBuilder);
        modelBuilder.ApplyConfiguration(new MembershipAuthorityConfiguration());
    }
}
