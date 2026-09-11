using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The tenant-filtered stance for a module context — a row is visible to the tenant(s) that own it, and to the
/// host. Composes the module's anemic configuration provider first, then the module's filter declarations — the
/// order is sealed so filters can never run before the model exists. The tenant-independent counterpart (same
/// provider, no tenancy) is <see cref="ReadDbContext"/>.
/// <para>
/// Single-owner and two-party rows share this one base: the stance a context takes is expressed by which helper
/// its <see cref="ApplyTenantFilters"/> calls, not by a separate base type.
/// </para>
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
    /// Declare which entities are filtered, and on which stance: single-owner
    /// (<see cref="Concertable.Kernel.ITenantScoped"/>) rows via
    /// <c>modelBuilder.ApplySingleOwner&lt;T&gt;(this)</c>, two-party venue↔artist
    /// (<see cref="Application.IVenueArtistTenantScoped"/>) rows via
    /// <c>modelBuilder.ApplyVenueArtist&lt;T&gt;(this)</c>. Deliberately NOT automatic off either marker:
    /// marked ≠ filtered is a per-entity product decision (a contract carries the owner but is read
    /// cross-tenant; a concert carries the pair but stays public).
    /// </summary>
    protected abstract void ApplyTenantFilters(ModelBuilder modelBuilder);
}
