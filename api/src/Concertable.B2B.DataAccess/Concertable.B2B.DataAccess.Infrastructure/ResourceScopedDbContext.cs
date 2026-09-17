using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The stance for a module whose rows are reached through resource access grants rather than through an
/// ownership column: a row is visible to a membership holding a live grant on it at the audience its own
/// permission reaches. The single-owner stance, where the row names its one owning tenant, stays on
/// <see cref="TenantScopedDbContext"/>, and a context may declare both.
/// <para>
/// Each entity's grant filter is declared in the owning context's <c>ApplyTenantFilters</c> against that
/// context's own grant set. It is deliberately not derived from a marker: which resources are reached by
/// grant is a per-entity product decision, and the filter has to name the grant family it reads.
/// </para>
/// </summary>
public abstract class ResourceScopedDbContext : TenantScopedDbContext, IHasResourceAccessContext
{
    public IResourceAccessContext ResourceAccess { get; }

    public DbSet<MembershipAuthority> MembershipAuthority => Set<MembershipAuthority>();

    public Guid? ActiveMembershipId => ResourceAccess.Membership?.MembershipId;
    public Guid? ActiveTenantId => ResourceAccess.Membership?.TenantId;
    public Guid? ActiveUserId => ResourceAccess.Membership?.UserId;
    public long? ActivePermissionVersion => ResourceAccess.Membership?.PermissionVersion;

    public ResourceAudience AudienceFor(string permission) => ResourceAccess.AudienceFor(permission);

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
