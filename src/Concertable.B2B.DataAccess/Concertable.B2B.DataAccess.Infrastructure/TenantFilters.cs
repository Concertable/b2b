using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The named "Tenant" query filter: its key (for <c>IgnoreQueryFilters([TenantFilters.Key])</c>)
/// and its per-entity registrations, called from a context's <c>OnModelCreating</c>. Whether an
/// entity is filtered is a per-entity product decision — a marked entity may stay public
/// (e.g. Concert, whose details page is marketplace browse).
/// </summary>
public static class TenantFilters
{
    public const string Key = "Tenant";

    /// <summary>
    /// The two-party filter: a row is visible to its venue tenant, its artist tenant, and the host.
    /// The lambda reads the tenant THROUGH the context instance (<paramref name="context"/> is the
    /// DbContext): EF caches the model once and re-binds context references per query, so a captured
    /// scoped <c>ITenantContext</c> would freeze the first request's tenant forever.
    /// </summary>
    public static void ApplyVenueArtist<TEntity>(this ModelBuilder modelBuilder, IHasTenantContext context)
        where TEntity : class, IVenueArtistTenantScoped =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(Key, e =>
            context.TenantContext.IsHost
            || e.VenueTenantId == context.TenantContext.TenantId
            || e.ArtistTenantId == context.TenantContext.TenantId);

    /// <summary>
    /// The single-owner filter: a row is visible to its owning tenant and the host. Same context-instance
    /// indirection as <see cref="ApplyVenueArtist{TEntity}"/> (the model is cached once, the tenant re-bound
    /// per query, so a captured scoped <c>ITenantContext</c> would freeze the first request's tenant forever).
    /// </summary>
    public static void ApplySingleOwner<TEntity>(this ModelBuilder modelBuilder, IHasTenantContext context)
        where TEntity : class, ITenantScoped =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(Key, e =>
            context.TenantContext.IsHost
            || e.TenantId == context.TenantContext.TenantId);
}
