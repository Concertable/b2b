using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The stance for a module whose rows are reached through resource access grants rather than an ownership
/// column. The single-owner stance stays on <see cref="TenantScopedDbContext"/>; a context may declare both.
/// </summary>
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
        IEntityTypeConfigurationProvider provider,
        ITenantContext tenantContext,
        IResourceAccessContext resourceAccess,
        string defaultSchema)
        : base(options, provider, tenantContext, defaultSchema)
    {
        ResourceAccess = resourceAccess;
    }

    protected override void ConfigureBorrowedRelations(ModelBuilder modelBuilder)
    {
        base.ConfigureBorrowedRelations(modelBuilder);
        modelBuilder.ApplyConfiguration(new MembershipAuthorityConfiguration());
    }
}
