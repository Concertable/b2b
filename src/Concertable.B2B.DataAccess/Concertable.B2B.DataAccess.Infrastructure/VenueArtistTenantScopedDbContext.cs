using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The tenant-filtered stance for a module context with two-party (venue↔artist) rows. Composes the
/// module's anemic configuration provider first, then the module's filter declarations — the order
/// is sealed so filters can never run before the model exists. The tenant-independent counterpart (same provider,
/// no tenancy) is <see cref="ReadDbContext"/>.
/// </summary>
public abstract class VenueArtistTenantScopedDbContext : DbContextBase, IHasTenantContext
{
    private readonly IEntityTypeConfigurationProvider provider;
    private readonly string defaultSchema;

    public ITenantContext TenantContext { get; }

    protected VenueArtistTenantScopedDbContext(
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
    /// Declare which two-party entities are filtered, via <c>modelBuilder.ApplyVenueArtist&lt;T&gt;(this)</c>.
    /// Deliberately NOT automatic off the <see cref="Application.IVenueArtistTenantScoped"/> marker:
    /// marked ≠ filtered is a per-entity product decision (a concert carries the pair but stays public).
    /// </summary>
    protected abstract void ApplyTenantFilters(ModelBuilder modelBuilder);
}
