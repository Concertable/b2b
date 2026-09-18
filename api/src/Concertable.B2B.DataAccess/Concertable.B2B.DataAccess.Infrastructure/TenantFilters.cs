using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The named "Tenant" query filter: its key (for <c>IgnoreQueryFilters([TenantFilters.Key])</c>) and the
/// single-owner registration, called from a context's <c>OnModelCreating</c>. Whether an entity is filtered
/// is a per-entity product decision. Grant-reached entities declare their own filter against their own grant
/// set in the owning context — see <see cref="AccessScopedDbContext"/>.
/// </summary>
public static class TenantFilters
{
    public const string Key = "Tenant";

    /// <summary>
    /// The single-owner filter: a row is visible to its owning tenant and the host. The lambda reads the tenant
    /// THROUGH the context instance, because the model is cached once and the tenant re-bound per query, so a
    /// captured scoped <c>ITenantContext</c> would freeze the first request's tenant forever.
    /// </summary>
    public static void ApplySingleOwner<TEntity>(this ModelBuilder modelBuilder, IHasTenantContext context)
        where TEntity : class, ITenantScoped =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(Key, e =>
            context.TenantContext.IsHost
            || e.TenantId == context.TenantContext.TenantId);
}
