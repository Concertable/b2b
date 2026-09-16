using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The tenant-filtered stance for a module context — a row is visible to the tenant that owns it, and to an
/// established trusted execution scope. Composes the module's anemic configuration provider first, then any
/// borrowed relations, then the module's filter declarations — the order is sealed so a filter can never run
/// before the model exists. The tenant-independent counterpart (same provider, no tenancy) is
/// <see cref="ReadDbContext"/>; the grant-reached counterpart is <see cref="AccessScopedDbContext"/>.
/// </summary>
public abstract class TenantScopedDbContext : DbContextBase, IHasTenantContext
{
    private readonly IEntityTypeConfigurationProvider provider;
    private readonly string defaultSchema;

    public ITenantContext TenantContext { get; }

    protected TenantScopedDbContext(
        DbContextOptions options,
        IEntityTypeConfigurationProvider provider,
        ITenantContext tenantContext,
        string defaultSchema)
        : base(options)
    {
        this.provider = provider;
        this.defaultSchema = defaultSchema;
        TenantContext = tenantContext;
    }

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(defaultSchema);
        provider.Configure(modelBuilder);
        ConfigureBorrowedRelations(modelBuilder);
        ApplyTenantFilters(modelBuilder);
    }

    /// <summary>
    /// Map relations another module owns and migrates, which this context only reads. Runs after the module's
    /// own configuration and before the filters, so a filter may reference a borrowed relation.
    /// </summary>
    protected virtual void ConfigureBorrowedRelations(ModelBuilder modelBuilder) { }

    /// <summary>
    /// Declare which entities are filtered, and on which stance. Single-owner rows, where the row names its
    /// one owning tenant, use <c>modelBuilder.ApplySingleOwner&lt;T&gt;(this)</c>; a grant-reached entity
    /// declares its own filter here against its module's own grant set — see
    /// <see cref="AccessScopedDbContext"/>. Deliberately NOT automatic off a marker: marked is not filtered,
    /// which is a per-entity product decision (a contract carries an owner but is read by the counterparty;
    /// a concert stays publicly browsable).
    /// </summary>
    protected abstract void ApplyTenantFilters(ModelBuilder modelBuilder);
}
