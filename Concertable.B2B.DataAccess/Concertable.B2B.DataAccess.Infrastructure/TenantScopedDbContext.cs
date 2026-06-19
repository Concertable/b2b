using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The tenant-filtered stance for a module context with single-owner (<see cref="Concertable.Kernel.ITenantScoped"/>)
/// rows — a row is visible to its owning tenant and the host. Composes the module's anemic configuration provider
/// first, then the module's filter declarations — the order is sealed so filters can never run before the model
/// exists. The public counterpart (same provider, no tenancy) is <see cref="PublicDbContext"/>; the two-party
/// sibling is <see cref="VenueArtistTenantDbContext"/>.
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
        ApplyTenantFilters(modelBuilder);
    }

    /// <summary>
    /// Declare which single-owner entities are filtered, via <c>modelBuilder.ApplySingleOwner&lt;T&gt;(this)</c>.
    /// Deliberately NOT automatic off the <see cref="Concertable.Kernel.ITenantScoped"/> marker:
    /// marked ≠ filtered is a per-entity product decision (a contract carries the owner but is read cross-tenant).
    /// </summary>
    protected abstract void ApplyTenantFilters(ModelBuilder modelBuilder);
}
