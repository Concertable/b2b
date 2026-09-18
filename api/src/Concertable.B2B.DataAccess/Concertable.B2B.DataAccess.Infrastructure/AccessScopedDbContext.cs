using Concertable.B2B.DataAccess.Application;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The stance for a module whose rows are reached through resource access grants rather than through an
/// ownership column: a row is visible to a tenant holding a live grant on it, and to an explicitly
/// established trusted execution scope. The single-owner stance, where the row names its one owning tenant,
/// stays on <see cref="TenantScopedDbContext"/>, and a context may declare both.
/// <para>
/// Each entity's grant filter is declared in the owning context's <c>ApplyTenantFilters</c> against that
/// context's own grant set. It is deliberately not derived from a marker: which resources are reached by
/// grant is a per-entity product decision, and the filter has to name the grant family it reads.
/// </para>
/// </summary>
public abstract class AccessScopedDbContext : TenantScopedDbContext, IHasAccessContext
{
    public IAccessContext AccessContext { get; }

    public DbSet<MembershipAuthorityFact> MembershipAuthority => Set<MembershipAuthorityFact>();

    protected AccessScopedDbContext(
        DbContextOptions options,
        IEntityTypeConfigurationProvider provider,
        ITenantContext tenantContext,
        IAccessContext accessContext,
        string defaultSchema)
        : base(options, provider, tenantContext, defaultSchema)
    {
        AccessContext = accessContext;
    }

    protected override void ConfigureBorrowedRelations(ModelBuilder modelBuilder)
    {
        base.ConfigureBorrowedRelations(modelBuilder);
        modelBuilder.ApplyConfiguration(new MembershipAuthorityFactConfiguration());
    }
}
